using System.Text.Json;
using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Common;
using B2BSpareParts.Application.DTOs.Orders.Invoices;
using B2BSpareParts.Application.DTOs.Pos;
using B2BSpareParts.Common;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Domain.Enums;
using B2BSpareParts.Infrastructure.Documents;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace B2BSpareParts.Infrastructure.Services;

public class PosService : IPosService
{
    private const string DefaultDisclaimerText = "Goods once sold are subject to store policy.";
    private const string DefaultAttestedStampText = "Attested / Authorized Stamp";

    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public PosService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PageResponse<PosProductListItemDto>> GetProductsAsync(GetPosProductsRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        EnsureStaffAccess();

        if (request.ShopId.HasValue)
        {
            await EnsureShopAsync(request.ShopId.Value, tenantId, ct);
        }

        var query = _db.Products
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.Sku.ToLower().Contains(search) ||
                (x.Barcode != null && x.Barcode.ToLower().Contains(search)) ||
                (x.Brand != null && x.Brand.Name.ToLower().Contains(search)) ||
                (x.Model != null && x.Model.Name.ToLower().Contains(search)) ||
                (x.PartType != null && x.PartType.Name.ToLower().Contains(search)));
        }

        var projected = query
            .ApplyCreatedAtSort(request)
            .Select(x => new PosProductListItemDto
            {
                ProductId = x.Id,
                ProductName = x.Name,
                Sku = x.Sku,
                Barcode = x.Barcode,
                BrandName = x.Brand != null ? x.Brand.Name : null,
                ModelName = x.Model != null ? x.Model.Name : null,
                PartTypeName = x.PartType != null ? x.PartType.Name : null,
                PrimaryImageUrl = x.Images.Where(i => i.IsPrimary).OrderBy(i => i.SortOrder).Select(i => i.FilePath).FirstOrDefault(),
                SellingPrice = x.DefaultSellingPrice,
                CurrencyCode = x.Tenant!.DefaultSellingCurrency!.Code,
                QuantityInHand = x.TrackingType == TrackingType.Serialized
                    ? _db.SerializedInventoryUnits.Count(u =>
                        u.TenantId == tenantId &&
                        u.ProductId == x.Id &&
                        u.Status == SerializedUnitStatus.InStock &&
                        !u.IsDeleted &&
                        (!request.ShopId.HasValue || u.ShopId == request.ShopId.Value))
                    : _db.ShopInventories
                        .Where(i =>
                            i.TenantId == tenantId &&
                            i.ProductId == x.Id &&
                            !i.IsDeleted &&
                            (!request.ShopId.HasValue || i.ShopId == request.ShopId.Value))
                        .Sum(i => i.QuantityOnHand),
                LowStockThreshold = x.LowStockThreshold,
                IsLowStock = (x.TrackingType == TrackingType.Serialized
                    ? _db.SerializedInventoryUnits.Count(u =>
                        u.TenantId == tenantId &&
                        u.ProductId == x.Id &&
                        u.Status == SerializedUnitStatus.InStock &&
                        !u.IsDeleted &&
                        (!request.ShopId.HasValue || u.ShopId == request.ShopId.Value))
                    : _db.ShopInventories
                        .Where(i =>
                            i.TenantId == tenantId &&
                            i.ProductId == x.Id &&
                            !i.IsDeleted &&
                            (!request.ShopId.HasValue || i.ShopId == request.ShopId.Value))
                        .Sum(i => i.QuantityOnHand)) <= x.LowStockThreshold
            })
            .Where(x => x.QuantityInHand > 0);

        var page = await projected.ToPageAsync(request, ct);
        var productIds = page.Items.Select(x => x.ProductId).Distinct().ToList();

        if (productIds.Count == 0)
            return page;

        var serializedBarcodes = await _db.SerializedInventoryUnits
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId &&
                        productIds.Contains(x.ProductId) &&
                        x.Status == SerializedUnitStatus.InStock &&
                        !x.IsDeleted &&
                        (!request.ShopId.HasValue || x.ShopId == request.ShopId.Value))
            .Select(x => new
            {
                x.ProductId,
                x.UnitBarcode,
                x.Imei1,
                x.Imei2
            })
            .ToListAsync(ct);

        var serializedBarcodeLookup = serializedBarcodes
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new ProductBarcodeDto
                {
                    Barcode = x.UnitBarcode,
                    Imei1 = x.Imei1 ?? string.Empty,
                    Imei2 = x.Imei2 ?? string.Empty
                }).ToList());

        foreach (var item in page.Items)
        {
            if (serializedBarcodeLookup.TryGetValue(item.ProductId, out var barcodes))
            {
                item.Barcodes = barcodes;
                continue;
            }

            item.Barcodes =
            [
                new ProductBarcodeDto
                {
                    Barcode = item.Barcode ?? string.Empty,
                    Imei1 = string.Empty,
                    Imei2 = string.Empty
                }
            ];
        }

        return page;
    }

    public async Task<CreatePosOrderResponseDto> CreateOrderAsync(CreatePosOrderRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _tenantContext.UserId;
        EnsureStaffAccess();

        if (request.Items == null || request.Items.Count == 0)
            throw new AppException("Order must have at least one item", 400);

        if (request.ShopId == Guid.Empty)
            throw new AppException("Shop is required", 400);

        var shop = await EnsureShopAsync(request.ShopId, tenantId, ct);

        Client? client;
        if (request.ClientId.HasValue)
        {
            client = await _db.Clients
                .FirstOrDefaultAsync(x => x.Id == request.ClientId.Value && x.TenantId == tenantId && !x.IsDeleted, ct)
                ?? throw new AppException("Client not found", 404);
        }
        else
        {
            client = await GetOrCreateWalkInClientAsync(tenantId, ct);
        }

        var aggregatedItems = request.Items
            .GroupBy(x => x.ProductId)
            .Select(g => new CreatePosOrderItemDto
            {
                ProductId = g.Key,
                Quantity = g.Sum(i => i.Quantity),
                Barcodes = g.SelectMany(i => i.Barcodes ?? []).ToList()
            })
            .ToList();

        if (aggregatedItems.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0))
            throw new AppException("All items must include a valid product and quantity greater than zero", 400);

        var tenant = await _db.Tenants
            .Include(x => x.DefaultSellingCurrency)
            .FirstOrDefaultAsync(x => x.Id == tenantId, ct)
            ?? throw new AppException("Tenant not found", 404);

        var theme = await _db.ThemeSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && !x.IsDeleted, ct);

        var productIds = aggregatedItems.Select(x => x.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Include(x => x.Brand)
            .Include(x => x.Model)
            .Include(x => x.PartType)
            .Include(x => x.Images)
            .Where(x => productIds.Contains(x.Id) && x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            .ToListAsync(ct);

        if (products.Count != productIds.Count)
            throw new AppException("One or more products were not found", 404);

        await using var transaction = await _db.Database.BeginTransactionAsync(ct);

        var order = new Order
        {
            TenantId = tenantId,
            ShopId = shop.Id,
            ClientId = client.Id,
            CurrencyId = tenant.DefaultSellingCurrencyId,
            ExchangeRate = 1m,
            OrderNumber = $"POS-{DateTime.UtcNow:yyyyMMddHHmmssfff}",
            Notes = request.Notes,
            PlacedByUserId = userId,
            PreparedByUserId = userId,
            Status = OrderStatus.Completed
        };

        foreach (var item in aggregatedItems)
        {
            var product = products.First(x => x.Id == item.ProductId);
            var selectedBarcodes = await GetSelectedSerializedBarcodesAsync(tenantId, shop.Id, product, item, ct);
            await EnsureStockAvailabilityAsync(tenantId, shop.Id, product, item.Quantity, ct, selectedBarcodes);

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.DefaultSellingPrice,
                BaseUnitPrice = product.DefaultSellingPrice,
                LineTotal = product.DefaultSellingPrice * item.Quantity,
                Product = product,
                SelectedUnitBarcodesJson = SerializeBarcodes(selectedBarcodes)
            });
        }

        order.Subtotal = order.Items.Sum(x => x.LineTotal);
        order.TotalAmount = order.Subtotal + order.TaxAmount - order.DiscountAmount;

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        foreach (var item in order.Items)
        {
            var product = products.First(x => x.Id == item.ProductId);
            await DeductStockAsync(order, item, product, ct);
        }

        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            Type = NotificationType.NewOrder,
            Title = "New POS Order",
            Message = $"POS order {order.OrderNumber} completed at {shop.Name}.",
            RelatedEntityId = order.Id
        });

        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return BuildCreateOrderResponse(order, shop.Name, client.BusinessName ?? client.Name, tenant.DefaultSellingCurrency?.Code ?? string.Empty, theme);
    }

    public async Task<OrderInvoicePdfDto> GetInvoicePdfAsync(Guid orderId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        EnsureStaffAccess();

        var order = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Shop)
            .Include(x => x.Client)
            .Include(x => x.Currency)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == orderId && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new AppException("Order not found", 404);

        var tenant = await _db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId, ct)
            ?? throw new AppException("Tenant not found", 404);

        var theme = await _db.ThemeSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && !x.IsDeleted, ct);

        var document = new PosInvoiceDocument(new PosInvoiceDocumentModel
        {
            TenantName = tenant.Name,
            ShopName = order.Shop?.Name ?? string.Empty,
            ShopAddress = order.Shop?.Address,
            ShopPhone = order.Shop?.Phone,
            OrderNumber = order.OrderNumber,
            CreatedAt = order.CreatedAt,
            CompletedAt = order.Status == OrderStatus.Completed ? order.UpdatedAt : null,
            ClientName = order.ClientId == Guid.Empty ? null : order.Client?.BusinessName ?? order.Client?.Name,
            CurrencyCode = order.Currency?.Code ?? string.Empty,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            BarcodeValue = order.OrderNumber,
            DisclaimerText = ResolveDisclaimer(theme),
            AttestedStampText = DefaultAttestedStampText,
            LogoPath = theme?.LogoPath,
            Items = order.Items.Select(i => new PosInvoiceDocumentItemModel
            {
                ProductName = i.Product?.Name ?? string.Empty,
                Sku = i.Product?.Sku ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        });

        return new OrderInvoicePdfDto
        {
            FileName = $"invoice-{order.OrderNumber}.pdf",
            Content = document.GeneratePdf()
        };
    }


    private async Task<Client> GetOrCreateWalkInClientAsync(Guid tenantId, CancellationToken ct)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.BusinessName == "Walk-in Customer" && !x.IsDeleted, ct);

        if (client != null)
            return client;

        client = new Client
        {
            TenantId = tenantId,
            Name = "Walk-in Customer",
            BusinessName = "Walk-in Customer",
            Status = ClientStatus.Approved
        };

        _db.Clients.Add(client);
        await _db.SaveChangesAsync(ct);
        return client;
    }

    private async Task<Shop> EnsureShopAsync(Guid shopId, Guid tenantId, CancellationToken ct)
    {
        var shop = await _db.Shops
            .FirstOrDefaultAsync(x => x.Id == shopId && x.TenantId == tenantId && x.IsActive && !x.IsDeleted, ct)
            ?? throw new AppException("Shop not found", 404);

        return shop;
    }

    private async Task EnsureStockAvailabilityAsync(Guid tenantId, Guid shopId, Product product, int quantity, CancellationToken ct, List<string>? selectedBarcodes = null)
    {
        if (product.TrackingType == TrackingType.Serialized)
        {
            if (selectedBarcodes is { Count: > 0 })
            {
                var normalizedBarcodes = NormalizeBarcodes(selectedBarcodes, quantity, product.Name);
                var selectedCount = await _db.SerializedInventoryUnits.CountAsync(x =>
                    x.TenantId == tenantId &&
                    x.ShopId == shopId &&
                    x.ProductId == product.Id &&
                    x.Status == SerializedUnitStatus.InStock &&
                    !x.IsDeleted &&
                    normalizedBarcodes.Contains(x.UnitBarcode), ct);

                if (selectedCount < quantity)
                    throw new AppException($"Insufficient serialized stock for {product.Name}", 400);

                return;
            }

            var availableCount = await _db.SerializedInventoryUnits.CountAsync(x =>
                x.TenantId == tenantId &&
                x.ShopId == shopId &&
                x.ProductId == product.Id &&
                x.Status == SerializedUnitStatus.InStock &&
                !x.IsDeleted, ct);

            if (availableCount < quantity)
                throw new AppException($"Insufficient serialized stock for {product.Name}", 400);

            return;
        }

        var stock = await _db.ShopInventories.FirstOrDefaultAsync(x =>
            x.TenantId == tenantId &&
            x.ShopId == shopId &&
            x.ProductId == product.Id &&
            !x.IsDeleted, ct);

        if (stock == null || stock.QuantityOnHand < quantity)
            throw new AppException($"Insufficient stock for {product.Name}", 400);
    }

    private async Task DeductStockAsync(Order order, OrderItem item, Product product, CancellationToken ct)
    {
        if (product.TrackingType == TrackingType.Serialized)
        {
            var selectedBarcodes = DeserializeBarcodes(item.SelectedUnitBarcodesJson);
            var unitsQuery = _db.SerializedInventoryUnits
                .Where(x =>
                    x.TenantId == order.TenantId &&
                    x.ShopId == order.ShopId &&
                    x.ProductId == item.ProductId &&
                    x.Status == SerializedUnitStatus.InStock &&
                    !x.IsDeleted)
                .OrderBy(x => x.CreatedAt);

            List<SerializedInventoryUnit> units;
            if (selectedBarcodes is { Count: > 0 })
            {
                var normalizedBarcodes = NormalizeBarcodes(selectedBarcodes, item.Quantity, product.Name);
                units = await unitsQuery
                    .Where(x => normalizedBarcodes.Contains(x.UnitBarcode))
                    .ToListAsync(ct);
            }
            else
            {
                units = await unitsQuery
                    .Take(item.Quantity)
                    .ToListAsync(ct);
            }

            if (units.Count < item.Quantity)
                throw new AppException($"Insufficient serialized stock for {product.Name}", 400);

            foreach (var unit in units)
            {
                unit.Status = SerializedUnitStatus.Sold;
                unit.SalePrice = item.UnitPrice;
                unit.UpdatedAt = DateTimeOffset.UtcNow;

                _db.StockMovements.Add(new StockMovement
                {
                    TenantId = order.TenantId,
                    ShopId = order.ShopId,
                    ProductId = item.ProductId,
                    SerializedInventoryUnitId = unit.Id,
                    MovementType = StockMovementType.Sale,
                    Quantity = 1,
                    ReferenceType = nameof(Order),
                    ReferenceId = order.Id,
                    PerformedByUserId = _tenantContext.UserId,
                    Note = "POS order fulfillment"
                });
            }

            return;
        }

        var stock = await _db.ShopInventories.FirstOrDefaultAsync(x =>
            x.TenantId == order.TenantId &&
            x.ShopId == order.ShopId &&
            x.ProductId == item.ProductId &&
            !x.IsDeleted, ct)
            ?? throw new AppException($"Stock not found for {product.Name}", 404);

        if (stock.QuantityOnHand < item.Quantity)
            throw new AppException($"Insufficient stock for {product.Name}", 400);

        stock.QuantityOnHand -= item.Quantity;
        stock.UpdatedAt = DateTimeOffset.UtcNow;

        _db.StockMovements.Add(new StockMovement
        {
            TenantId = order.TenantId,
            ShopId = order.ShopId,
            ProductId = item.ProductId,
            MovementType = StockMovementType.Sale,
            Quantity = item.Quantity,
            ReferenceType = nameof(Order),
            ReferenceId = order.Id,
            PerformedByUserId = _tenantContext.UserId,
            Note = "POS order fulfillment"
        });

        if (stock.QuantityOnHand <= stock.LowStockThreshold)
        {
            var exists = await _db.Notifications.AnyAsync(x =>
                x.TenantId == order.TenantId &&
                x.Type == NotificationType.LowStock &&
                x.RelatedEntityId == product.Id &&
                !x.IsDeleted, ct);

            if (!exists)
            {
                _db.Notifications.Add(new Notification
                {
                    TenantId = order.TenantId,
                    Type = NotificationType.LowStock,
                    Title = "Low stock alert",
                    Message = $"{product.Name} is low in stock at shop {order.ShopId}",
                    RelatedEntityId = product.Id
                });
            }
        }
    }


    private async Task<List<string>?> GetSelectedSerializedBarcodesAsync(Guid tenantId, Guid shopId, Product product, CreatePosOrderItemDto item, CancellationToken ct)
    {
        if (product.TrackingType != TrackingType.Serialized)
            return null;

        if (item.Barcodes == null || item.Barcodes.Count == 0)
            throw new AppException($"Barcodes are required for serialized product {product.Name}", 400);

        var normalizedBarcodes = NormalizeBarcodes(item.Barcodes, item.Quantity, product.Name);
        var matchedCount = await _db.SerializedInventoryUnits.CountAsync(x =>
            x.TenantId == tenantId &&
            x.ShopId == shopId &&
            x.ProductId == product.Id &&
            x.Status == SerializedUnitStatus.InStock &&
            !x.IsDeleted &&
            normalizedBarcodes.Contains(x.UnitBarcode), ct);

        if (matchedCount != normalizedBarcodes.Count)
            throw new AppException($"One or more serialized barcodes were not found in stock for {product.Name}", 400);

        return normalizedBarcodes;
    }

    private static List<string> NormalizeBarcodes(IEnumerable<string> barcodes, int expectedQuantity, string productName)
    {
        var normalized = barcodes
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Count != expectedQuantity)
            throw new AppException($"Barcodes count must match quantity {expectedQuantity} for {productName}", 400);

        return normalized;
    }

    private static string? SerializeBarcodes(List<string>? barcodes)
        => barcodes is { Count: > 0 } ? JsonSerializer.Serialize(barcodes) : null;

    private static List<string>? DeserializeBarcodes(string? barcodesJson)
        => string.IsNullOrWhiteSpace(barcodesJson)
            ? null
            : JsonSerializer.Deserialize<List<string>>(barcodesJson);

    private CreatePosOrderResponseDto BuildCreateOrderResponse(Order order, string shopName, string? clientName, string currencyCode, ThemeSetting? theme)
    {
        return new CreatePosOrderResponseDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            CreatedAt = order.CreatedAt,
            CompletedAt = order.Status == OrderStatus.Completed ? order.UpdatedAt : null,
            ShopName = shopName,
            ClientName = clientName,
            CurrencyCode = currencyCode,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            InvoicePdfUrl = $"/api/pos/orders/{order.Id}/invoice-pdf",
            BarcodeValue = order.OrderNumber,
            LogoUrl = theme?.LogoPath,
            DisclaimerText = ResolveDisclaimer(theme),
            AttestedStampText = DefaultAttestedStampText,
            Items = order.Items.Select(i => new CreatePosOrderResponseItemDto
            {
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Sku = i.Product?.Sku ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }

    private static string ResolveDisclaimer(ThemeSetting? theme)
        => string.IsNullOrWhiteSpace(theme?.FooterText) ? DefaultDisclaimerText : theme.FooterText!;

    private void EnsureStaffAccess()
    {
        if (_tenantContext.TenantId == Guid.Empty)
            throw new AppException("Tenant context missing", 400);

        if (_tenantContext.Role != UserRoles.Owner && _tenantContext.Role != UserRoles.Staff)
            throw new AppException("You are not authorized to access POS operations", 403);
    }
}
