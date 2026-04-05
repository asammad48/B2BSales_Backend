using System.Text.Json;
using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Orders;
using B2BSpareParts.Application.DTOs.Orders.ClientOrders;
using B2BSpareParts.Common;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Domain.Enums;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public OrderService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PageResponse<OrderListItemResponseDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _db.Orders
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.OrderNumber.ToLower().Contains(search) || x.Client!.BusinessName.ToLower().Contains(search) || x.Shop!.Name.ToLower().Contains(search));
        }

        var projected = query
            .ApplyCreatedAtSort(request)
            .Select(x => new OrderListItemResponseDto
            {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                ShopId = x.ShopId,
                ShopName = x.Shop!.Name,
                ClientId = x.ClientId,
                ClientName = x.Client!.BusinessName,
                Status = x.Status.ToString(),
                CurrencyId = x.CurrencyId,
                CurrencyCode = x.Currency!.Code,
                TotalAmount = x.TotalAmount,
                CreatedAt = x.CreatedAt
            });

        return await projected.ToPageAsync(request, ct);
    }

    public async Task<OrderDetailsDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var order = await _db.Orders
            .AsNoTracking()
            .Include(x => x.Client)
            .Include(x => x.Shop)
            .Include(x => x.Currency)
            .Include(x => x.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new AppException("Order not found", 404);

        return new OrderDetailsDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            ClientId = order.ClientId,
            ClientName = order.Client!.Name,
            BusinessName = order.Client.BusinessName,
            ShopId = order.ShopId,
            ShopName = order.Shop!.Name,
            Status = order.Status.ToString(),
            StatusLabel = order.Status.ToString(),
            CurrencyCode = order.Currency!.Code,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            TaxAmount = order.TaxAmount,
            TotalAmount = order.TotalAmount,
            Notes = order.Notes,
            CreatedAt = order.CreatedAt,
            ReadyAt = order.Status >= OrderStatus.ReadyForPickup ? order.UpdatedAt : null,
            CompletedAt = order.Status == OrderStatus.Completed ? order.UpdatedAt : null,
            Items = order.Items.Select(i => new OrderDetailsItemDto
            {
                OrderItemId = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product!.Name,
                Sku = i.Product.Sku,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }

    public async Task<PageResponse<ClientOrderListItemDto>> GetClientOrdersAsync(Guid clientId, PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        if (_tenantContext.Role == UserRoles.Client)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == _tenantContext.UserId && u.TenantId == tenantId && !u.IsDeleted, ct)
                ?? throw new AppException("User not found", 404);
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Email == user.Email && c.TenantId == tenantId && !c.IsDeleted, ct)
                ?? throw new AppException("Client profile not found", 404);

            if (client.Id != clientId)
                throw new AppException("Unauthorized to view these orders", 403);
        }

        var query = _db.Orders
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.ClientId == clientId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.OrderNumber.ToLower().Contains(search) || x.Shop!.Name.ToLower().Contains(search));
        }

        var projected = query
            .ApplyCreatedAtSort(request)
            .Select(x => new ClientOrderListItemDto
            {
                OrderId = x.Id,
                OrderNumber = x.OrderNumber,
                ClientId = x.ClientId,
                ShopId = x.ShopId,
                ShopName = x.Shop!.Name,
                Status = x.Status,
                CurrencyCode = x.Currency!.Code,
                Subtotal = x.Subtotal,
                DiscountAmount = x.DiscountAmount,
                TaxAmount = x.TaxAmount,
                TotalAmount = x.TotalAmount,
                CreatedAt = x.CreatedAt,
                Notes = x.Notes
            });

        return await projected.ToPageAsync(request, ct);
    }

    public async Task<ClientOrderSummaryDto> GetClientOrderSummaryAsync(Guid clientId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        if (_tenantContext.Role == UserRoles.Client)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == _tenantContext.UserId && u.TenantId == tenantId && !u.IsDeleted, ct)
                ?? throw new AppException("User not found", 404);
            var client = await _db.Clients.FirstOrDefaultAsync(c => c.Email == user.Email && c.TenantId == tenantId && !c.IsDeleted, ct)
                ?? throw new AppException("Client profile not found", 404);

            if (client.Id != clientId)
                throw new AppException("Unauthorized to view this summary", 403);
        }

        var orders = await _db.Orders
            .Where(x => x.TenantId == tenantId && x.ClientId == clientId && !x.IsDeleted)
            .Select(x => x.Status)
            .ToListAsync(ct);

        return new ClientOrderSummaryDto
        {
            ClientId = clientId,
            TotalOrders = orders.Count,
            CompletedOrders = orders.Count(x => x == OrderStatus.Completed),
            PendingOrders = orders.Count(x => x == OrderStatus.Pending),
            ReadyForPickupOrders = orders.Count(x => x == OrderStatus.ReadyForPickup),
            CancelledOrders = orders.Count(x => x == OrderStatus.Cancelled),
            UnableToFulfillOrders = orders.Count(x => x == OrderStatus.UnableToFulfill)
        };
    }

    public async Task<Guid> CreateAsync(CreateOrderRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var client = await _db.Clients.FirstOrDefaultAsync(x => x.Id == request.ClientId && x.TenantId == tenantId && !x.IsDeleted, ct)
                     ?? throw new AppException("Client not found", 404);
        if (client.Status != ClientStatus.Approved)
            throw new AppException("Client is not approved");

        if (request.Items.Count == 0)
            throw new AppException("Order must have at least one item");

        var products = await _db.Products.Where(x => request.Items.Select(i => i.ProductId).Contains(x.Id) && x.TenantId == tenantId && !x.IsDeleted).ToListAsync(ct);
        var order = new Order
        {
            TenantId = tenantId,
            ShopId = request.ShopId,
            ClientId = request.ClientId,
            CurrencyId = request.CurrencyId,
            ExchangeRate = request.ExchangeRate,
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Notes = request.Notes,
            PlacedByUserId = _tenantContext.UserId
        };

        foreach (var item in request.Items)
        {
            var product = products.FirstOrDefault(x => x.Id == item.ProductId) ?? throw new AppException($"Product {item.ProductId} not found");
            ValidateRequestedQuantity(item.Quantity, product.Name);

            var selectedBarcodes = await GetSelectedSerializedBarcodesForCreateAsync(product, request.ShopId, item, tenantId, ct);
            await EnsureStockAvailableForOrderItemAsync(product, request.ShopId, item.Quantity, tenantId, ct, selectedBarcodes);

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.DefaultSellingPrice,
                BaseUnitPrice = product.DefaultSellingPrice,
                LineTotal = product.DefaultSellingPrice * item.Quantity,
                SelectedUnitBarcodesJson = SerializeBarcodes(selectedBarcodes)
            });
        }

        order.Subtotal = order.Items.Sum(x => x.LineTotal);
        order.TotalAmount = order.Subtotal + order.TaxAmount - order.DiscountAmount;
        _db.Orders.Add(order);

        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            Type = NotificationType.NewOrder,
            Title = "New Order",
            Message = $"New order {order.OrderNumber} created for {client.BusinessName}.",
            RelatedEntityId = order.Id
        });

        await _db.SaveChangesAsync(ct);

        return order.Id;
    }

    public async Task<PlaceClientOrderResponseDto> PlaceClientOrderAsync(PlaceClientOrderRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _tenantContext.UserId;

        var user = await _db.Users
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Id == userId && u.TenantId == tenantId && !u.IsDeleted, ct)
            ?? throw new AppException("User not found", 404);

        if (user.Role != UserRoles.Client)
            throw new AppException("Only clients can place orders through this API", 403);

        var client = await _db.Clients.FirstOrDefaultAsync(x => x.Email == user.Email && x.TenantId == tenantId && !x.IsDeleted, ct)
                     ?? throw new AppException("Client profile not found", 404);

        if (client.Status != ClientStatus.Approved)
            throw new AppException("Client is not approved", 403);

        if (request.Items == null || request.Items.Count == 0)
            throw new AppException("Order must have at least one item", 400);

        var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(x => productIds.Contains(x.Id) && x.TenantId == tenantId && !x.IsDeleted)
            .ToListAsync(ct);

        var requestedItems = request.Items
            .GroupBy(x => x.ProductId)
            .Select(g => new RequestedOrderItem(g.Key, g.Sum(x => x.Quantity)))
            .ToList();

        foreach (var item in requestedItems)
        {
            var product = products.FirstOrDefault(x => x.Id == item.ProductId)
                          ?? throw new AppException($"Product {item.ProductId} not found", 404);

            if (!product.IsActive)
                throw new AppException($"Product {product.Name} is not available for order", 400);

            ValidateRequestedQuantity(item.Quantity, product.Name);
        }

        var autoSelectedSerializedBarcodes = await GetAutoSelectedSerializedBarcodesAsync(
            products.Where(x => x.TrackingType == TrackingType.Serializado).ToList(),
            request.ShopId,
            requestedItems,
            tenantId,
            ct);

        var order = new Order
        {
            TenantId = tenantId,
            ShopId = request.ShopId,
            ClientId = client.Id,
            CurrencyId = client.PreferredCurrencyId ?? user.Tenant!.DefaultSellingCurrencyId,
            ExchangeRate = 1.0m,
            OrderNumber = $"ORD-CL-{DateTime.UtcNow:yyyyMMddHHmmss}",
            Notes = request.Notes,
            PlacedByUserId = userId,
            Status = OrderStatus.Pending
        };

        foreach (var item in requestedItems)
        {
            var product = products.First(x => x.Id == item.ProductId);
            var selectedBarcodes = autoSelectedSerializedBarcodes.TryGetValue(item.ProductId, out var barcodes)
                ? barcodes
                : null;

            await EnsureStockAvailableForOrderItemAsync(product, request.ShopId, item.Quantity, tenantId, ct, selectedBarcodes);

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.DefaultSellingPrice,
                BaseUnitPrice = product.DefaultSellingPrice,
                LineTotal = product.DefaultSellingPrice * item.Quantity,
                SelectedUnitBarcodesJson = SerializeBarcodes(selectedBarcodes)
            });
        }

        order.Subtotal = order.Items.Sum(x => x.LineTotal);
        order.TotalAmount = order.Subtotal + order.TaxAmount - order.DiscountAmount;

        _db.Orders.Add(order);

        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            Type = NotificationType.NewOrder,
            Title = "New Client Order",
            Message = $"New client order {order.OrderNumber} placed by {client.BusinessName}.",
            RelatedEntityId = order.Id
        });

        await _db.SaveChangesAsync(ct);

        return new PlaceClientOrderResponseDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Status = order.Status.ToString(),
            Message = "Order placed successfully."
        };
    }

    public async Task MarkReadyAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var order = await _db.Orders
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
                    ?? throw new AppException("Order not found", 404);

        if (order.Status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.UnableToFulfill)
            throw new AppException("Order cannot be marked ready in its current state");

        await EnsureOrderCanBeFulfilledAsync(order, tenantId, ct);

        order.Status = OrderStatus.ReadyForPickup;
        order.PreparedByUserId = _tenantContext.UserId;
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            Type = NotificationType.OrderReady,
            Title = "Order ready",
            Message = $"Order {order.OrderNumber} is ready for pickup.",
            RelatedEntityId = order.Id
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var order = await _db.Orders
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new AppException("Order not found", 404);

        if (order.Status == OrderStatus.Completed)
            return;

        if (order.Status is OrderStatus.Cancelled or OrderStatus.UnableToFulfill)
            throw new AppException("Order cannot be completed in its current state");

        foreach (var item in order.Items)
        {
            var product = item.Product ?? await _db.Products.FirstAsync(x => x.Id == item.ProductId && x.TenantId == tenantId && !x.IsDeleted, ct);
            if (product.TrackingType == TrackingType.Serializado)
            {
                var selectedBarcodes = DeserializeBarcodes(item.SelectedUnitBarcodesJson);
                var units = await GetSerializedUnitsForCompletionAsync(order.ShopId, item.ProductId, item.Quantity, tenantId, selectedBarcodes, ct);

                if (units.Count < item.Quantity)
                {
                    await MoveOrderToUnableToFulfillAsync(order, product, "serialized stock", ct);
                    throw new AppException($"Order moved to UnableToFulfill. Insufficient serialized stock for {product.Name}.");
                }

                foreach (var unit in units)
                {
                    unit.Status = SerializedUnitStatus.Sold;
                    unit.UpdatedAt = DateTimeOffset.UtcNow;
                    _db.StockMovements.Add(new StockMovement
                    {
                        TenantId = tenantId,
                        ShopId = order.ShopId,
                        ProductId = item.ProductId,
                        SerializedInventoryUnitId = unit.Id,
                        MovementType = StockMovementType.Sale,
                        Quantity = 1,
                        ReferenceType = nameof(Order),
                        ReferenceId = order.Id,
                        PerformedByUserId = _tenantContext.UserId
                    });
                }
            }
            else
            {
                var stock = await _db.ShopInventories.Include(x => x.Product).FirstOrDefaultAsync(x =>
                    x.TenantId == tenantId && x.ShopId == order.ShopId && x.ProductId == item.ProductId && !x.IsDeleted, ct)
                    ?? throw new AppException($"Stock not found for {product.Name}");

                if (stock.QuantityOnHand < item.Quantity)
                {
                    await MoveOrderToUnableToFulfillAsync(order, product, "stock", ct);
                    throw new AppException($"Order moved to UnableToFulfill. Insufficient stock for {product.Name}.");
                }

                stock.QuantityOnHand -= item.Quantity;
                stock.UpdatedAt = DateTimeOffset.UtcNow;

                _db.StockMovements.Add(new StockMovement
                {
                    TenantId = tenantId,
                    ShopId = order.ShopId,
                    ProductId = item.ProductId,
                    MovementType = StockMovementType.Sale,
                    Quantity = item.Quantity,
                    ReferenceType = nameof(Order),
                    ReferenceId = order.Id,
                    PerformedByUserId = _tenantContext.UserId
                });

                if (stock.QuantityOnHand <= stock.LowStockThreshold)
                {
                    var exists = await _db.Notifications.AnyAsync(x =>
                        x.TenantId == tenantId &&
                        x.Type == NotificationType.LowStock &&
                        x.RelatedEntityId == item.ProductId &&
                        !x.IsDeleted, ct);

                    if (!exists)
                    {
                        _db.Notifications.Add(new Notification
                        {
                            TenantId = tenantId,
                            Type = NotificationType.LowStock,
                            Title = "Low stock alert",
                            Message = $"{product.Name} is low in stock at shop {order.ShopId}",
                            RelatedEntityId = product.Id
                        });
                    }
                }
            }
        }

        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task MarkUnableToFulfillAsync(Guid id, string? reason = null, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var order = await _db.Orders
            .Include(x => x.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
                    ?? throw new AppException("Order not found", 404);

        if (order.Status == OrderStatus.Completed)
            throw new AppException("Completed order cannot be moved to UnableToFulfill");


        order.Status = OrderStatus.UnableToFulfill;
        order.Notes = string.IsNullOrWhiteSpace(reason)
            ? order.Notes
            : string.IsNullOrWhiteSpace(order.Notes) ? reason : order.Notes + $" | {reason}";
        order.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            Type = NotificationType.OrderUnableToFulfill,
            Title = "Order Unable to Fulfill",
            Message = $"Order {order.OrderNumber} marked as unable to fulfill. Reason: {reason}",
            RelatedEntityId = order.Id
        });

        await _db.SaveChangesAsync(ct);
    }

    private async Task<List<string>?> GetSelectedSerializedBarcodesForCreateAsync(Product product, Guid shopId, CreateOrderItemRequestDto item, Guid tenantId, CancellationToken ct)
    {
        if (product.TrackingType != TrackingType.Serializado)
            return null;

        if (item.Barcodes == null || item.Barcodes.Count == 0)
            throw new AppException($"Barcodes are required for serialized product {product.Name}", 400);

        var normalizedBarcodes = NormalizeBarcodes(item.Barcodes, item.Quantity, product.Name);
        var matchedCount = await _db.SerializedInventoryUnits.CountAsync(x =>
            x.TenantId == tenantId &&
            x.ShopId == shopId &&
            x.ProductId == item.ProductId &&
            x.Status == SerializedUnitStatus.InStock &&
            !x.IsDeleted &&
            normalizedBarcodes.Contains(x.UnitBarcode), ct);

        if (matchedCount != normalizedBarcodes.Count)
            throw new AppException($"One or more serialized barcodes were not found in stock for {product.Name}", 400);

        return normalizedBarcodes;
    }

    private async Task<Dictionary<Guid, List<string>>> GetAutoSelectedSerializedBarcodesAsync(
        List<Product> serializedProducts,
        Guid shopId,
        List<RequestedOrderItem> requestedItems,
        Guid tenantId,
        CancellationToken ct)
    {
        var result = new Dictionary<Guid, List<string>>();
        if (serializedProducts.Count == 0)
            return result;

        var serializedProductIds = serializedProducts.Select(x => x.Id).ToHashSet();
        var requestedSerializedItems = requestedItems.Where(x => serializedProductIds.Contains(x.ProductId)).ToList();
        if (requestedSerializedItems.Count == 0)
            return result;

        var serializedUnits = await _db.SerializedInventoryUnits
            .Where(x => x.TenantId == tenantId && x.ShopId == shopId && serializedProductIds.Contains(x.ProductId) && x.Status == SerializedUnitStatus.InStock && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.UnitBarcode)
            .Select(x => new { x.ProductId, x.UnitBarcode })
            .ToListAsync(ct);

        foreach (var item in requestedSerializedItems)
        {
            var selected = serializedUnits
                .Where(x => x.ProductId == item.ProductId)
                .Take(item.Quantity)
                .Select(x => x.UnitBarcode)
                .ToList();

            var product = serializedProducts.First(x => x.Id == item.ProductId);
            if (selected.Count < item.Quantity)
                throw new AppException($"Insufficient serialized stock for {product.Name}", 400);

            result[item.ProductId] = selected;
        }

        return result;
    }

    private async Task EnsureOrderCanBeFulfilledAsync(Order order, Guid tenantId, CancellationToken ct)
    {
        foreach (var item in order.Items)
        {
            var product = item.Product ?? await _db.Products.FirstAsync(x => x.Id == item.ProductId && x.TenantId == tenantId && !x.IsDeleted, ct);
            var selectedBarcodes = DeserializeBarcodes(item.SelectedUnitBarcodesJson);
            await EnsureStockAvailableForOrderItemAsync(product, order.ShopId, item.Quantity, tenantId, ct, selectedBarcodes);
        }
    }

    private async Task EnsureStockAvailableForOrderItemAsync(Product product, Guid shopId, int quantity, Guid tenantId, CancellationToken ct, List<string>? selectedBarcodes = null)
    {
        if (product.TrackingType == TrackingType.Serializado)
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
        if (stock is null || stock.QuantityOnHand < quantity)
            throw new AppException($"Insufficient stock for {product.Name}", 400);
    }

    private async Task<List<SerializedInventoryUnit>> GetSerializedUnitsForCompletionAsync(Guid shopId, Guid productId, int quantity, Guid tenantId, List<string>? selectedBarcodes, CancellationToken ct)
    {
        if (selectedBarcodes is { Count: > 0 })
        {
            var normalizedBarcodes = NormalizeBarcodes(selectedBarcodes, quantity, productId.ToString());
            return await _db.SerializedInventoryUnits
                .Where(x => x.TenantId == tenantId && x.ShopId == shopId && x.ProductId == productId && x.Status == SerializedUnitStatus.InStock && !x.IsDeleted && normalizedBarcodes.Contains(x.UnitBarcode))
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.UnitBarcode)
                .ToListAsync(ct);
        }

        return await _db.SerializedInventoryUnits
            .Where(x => x.TenantId == tenantId && x.ShopId == shopId && x.ProductId == productId && x.Status == SerializedUnitStatus.InStock && !x.IsDeleted)
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.UnitBarcode)
            .Take(quantity)
            .ToListAsync(ct);
    }

    private async Task MoveOrderToUnableToFulfillAsync(Order order, Product product, string stockLabel, CancellationToken ct)
    {
        order.Status = OrderStatus.UnableToFulfill;
        order.Notes = string.IsNullOrWhiteSpace(order.Notes)
            ? $"Unable to fulfill at pickup confirmation due to insufficient {stockLabel} for {product.Name}."
            : order.Notes + $" | Unable to fulfill: insufficient {stockLabel} for {product.Name}.";
        order.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateRequestedQuantity(int quantity, string productName)
    {
        if (quantity <= 0)
            throw new AppException($"Quantity for product {productName} must be greater than zero", 400);
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

    private sealed record RequestedOrderItem(Guid ProductId, int Quantity);
}
