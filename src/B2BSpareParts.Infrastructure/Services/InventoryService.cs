using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Common;
using B2BSpareParts.Application.DTOs.Inventory;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Domain.Enums;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public InventoryService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PageResponse<InventoryListItemResponseDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var search = request.Search?.Trim().ToLower();

        var quantityItems = (await _db.ShopInventories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.Product!.TrackingType == TrackingType.QuantityBased)
            .Where(x => search == null ||
                        x.Product!.Name.ToLower().Contains(search) ||
                        x.Product.Sku.ToLower().Contains(search) ||
                        (x.Product.Barcode != null && x.Product.Barcode.ToLower().Contains(search)) ||
                        x.Shop!.Name.ToLower().Contains(search))
            .Select(x => new
            {
                x.ProductId,
                ProductName = x.Product!.Name,
                x.ShopId,
                ShopName = x.Shop!.Name,
                x.Product.BrandId,
                BrandName = x.Product.Brand != null ? x.Product.Brand.Name : null,
                x.Product.ModelId,
                ModelName = x.Product.Model != null ? x.Product.Model.Name : null,
                x.Product.Sku,
                ProductBarcode = x.Product.Barcode,
                TrackingType = x.Product.TrackingType.ToString(),
                x.QuantityOnHand,
                x.ReservedQuantity,
                x.LowStockThreshold
            })
            .ToListAsync(ct))
            .Select(x => new InventoryListItemResponseDto
            {
                ProductId = x.ProductId,
                ProductName = x.ProductName,
                ShopId = x.ShopId,
                ShopName = x.ShopName,
                BrandId = x.BrandId,
                BrandName = x.BrandName,
                ModelId = x.ModelId,
                ModelName = x.ModelName,
                Sku = x.Sku,
                Barcode = x.ProductBarcode,
                Barcodes =
                [
                    new ProductBarcodeDto
                    {
                        Barcode = x.ProductBarcode ?? string.Empty,
                        Imei1 = string.Empty,
                        Imei2 = string.Empty
                    }
                ],
                TrackingType = x.TrackingType,
                QuantityOnHand = x.QuantityOnHand,
                ReservedQuantity = x.ReservedQuantity,
                AvailableQuantity = x.QuantityOnHand - x.ReservedQuantity,
                LowStockThreshold = x.LowStockThreshold
            })
            .ToList();

        var serializedUnits = await _db.SerializedInventoryUnits
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .Where(x => x.Status == SerializedUnitStatus.InStock || x.Status == SerializedUnitStatus.Reserved)
            .Where(x => search == null ||
                        x.Product!.Name.ToLower().Contains(search) ||
                        x.Product.Sku.ToLower().Contains(search) ||
                        (x.Product.Barcode != null && x.Product.Barcode.ToLower().Contains(search)) ||
                        x.Shop!.Name.ToLower().Contains(search) ||
                        x.UnitBarcode.ToLower().Contains(search) ||
                        (x.Imei1 != null && x.Imei1.ToLower().Contains(search)) ||
                        (x.Imei2 != null && x.Imei2.ToLower().Contains(search)))
            .Select(x => new
            {
                x.ProductId,
                ProductName = x.Product!.Name,
                x.ShopId,
                ShopName = x.Shop!.Name,
                x.Product.BrandId,
                BrandName = x.Product.Brand != null ? x.Product.Brand.Name : null,
                x.Product.ModelId,
                ModelName = x.Product.Model != null ? x.Product.Model.Name : null,
                x.Product.Sku,
                ProductBarcode = x.Product.Barcode,
                x.Product.LowStockThreshold,
                x.Status,
                x.UnitBarcode,
                x.Imei1,
                x.Imei2
            })
            .ToListAsync(ct);

        var serializedItems = serializedUnits
            .GroupBy(x => new
            {
                x.ProductId,
                x.ProductName,
                x.ShopId,
                x.ShopName,
                x.BrandId,
                x.BrandName,
                x.ModelId,
                x.ModelName,
                x.Sku,
                x.ProductBarcode,
                x.LowStockThreshold
            })
            .Select(g =>
            {
                var reservedQuantity = g.Count(x => x.Status == SerializedUnitStatus.Reserved);
                var availableQuantity = g.Count(x => x.Status == SerializedUnitStatus.InStock);

                return new InventoryListItemResponseDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    ShopId = g.Key.ShopId,
                    ShopName = g.Key.ShopName,
                    BrandId = g.Key.BrandId,
                    BrandName = g.Key.BrandName,
                    ModelId = g.Key.ModelId,
                    ModelName = g.Key.ModelName,
                    Sku = g.Key.Sku,
                    Barcode = g.Key.ProductBarcode,
                    Barcodes = g.Select(x => new ProductBarcodeDto
                    {
                        Barcode = x.UnitBarcode,
                        Imei1 = x.Imei1 ?? string.Empty,
                        Imei2 = x.Imei2 ?? string.Empty
                    }).ToList(),
                    TrackingType = TrackingType.Serialized.ToString(),
                    QuantityOnHand = g.Count(),
                    ReservedQuantity = reservedQuantity,
                    AvailableQuantity = availableQuantity,
                    LowStockThreshold = g.Key.LowStockThreshold
                };
            })
            .ToList();

        var combinedItems = quantityItems
            .Concat(serializedItems)
            .OrderBy(x => x.ProductName)
            .ThenBy(x => x.ShopName)
            .ToList();

        var totalCount = combinedItems.Count;
        var pageItems = combinedItems
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PageResponse<InventoryListItemResponseDto>
        {
            Items = pageItems,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }

    public async Task StockInAsync(StockInRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId && x.TenantId == tenantId && !x.IsDeleted, ct)
                      ?? throw new AppException("Product not found", 404);

        if (request.PricingMode == PricingMode.PercentageBased && request.MarkupPercentage is null)
            throw new AppException("Markup percentage is required for percentage-based pricing");

        if (request.Quantity <= 0)
            throw new AppException("Quantity must be greater than zero");

        var finalSellingPrice = request.SellingPrice ??
                                (request.PricingMode == PricingMode.PercentageBased
                                    ? request.BuyingPrice + (request.BuyingPrice * (request.MarkupPercentage ?? 0) / 100m)
                                    : product.DefaultSellingPrice);

        if (product.TrackingType == TrackingType.Serialized)
        {
            ValidateSerializedUnits(request.SerializedUnits, request.Quantity, requireExactQuantityMatch: true);

            foreach (var unit in request.SerializedUnits)
            {
                var serializedUnit = new SerializedInventoryUnit
                {
                    TenantId = tenantId,
                    ShopId = request.ShopId,
                    ProductId = request.ProductId,
                    UnitBarcode = unit.UnitBarcode.Trim(),
                    SerialNumber = unit.SerialNumber?.Trim(),
                    Imei1 = unit.Imei1?.Trim(),
                    Imei2 = unit.Imei2?.Trim(),
                    PurchaseCost = request.BuyingPrice,
                    SalePrice = unit.SalePrice ?? finalSellingPrice
                };

                _db.SerializedInventoryUnits.Add(serializedUnit);
                _db.StockMovements.Add(new StockMovement
                {
                    TenantId = tenantId,
                    ShopId = request.ShopId,
                    ProductId = request.ProductId,
                    SerializedInventoryUnitId = serializedUnit.Id,
                    MovementType = StockMovementType.StockIn,
                    Quantity = 1,
                    Note = "Serialized stock in",
                    PerformedByUserId = _tenantContext.UserId
                });
            }
        }
        else
        {
            var stock = await _db.ShopInventories.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ShopId == request.ShopId && x.ProductId == request.ProductId && !x.IsDeleted, ct);
            if (stock is null)
            {
                stock = new ShopInventory
                {
                    TenantId = tenantId,
                    ShopId = request.ShopId,
                    ProductId = request.ProductId,
                    QuantityOnHand = 0,
                    ReservedQuantity = 0,
                    LowStockThreshold = product.LowStockThreshold
                };
                _db.ShopInventories.Add(stock);
            }

            stock.QuantityOnHand += request.Quantity;
            stock.UpdatedAt = DateTimeOffset.UtcNow;

            _db.StockMovements.Add(new StockMovement
            {
                TenantId = tenantId,
                ShopId = request.ShopId,
                ProductId = request.ProductId,
                MovementType = StockMovementType.StockIn,
                Quantity = request.Quantity,
                Note = "Quantity stock in",
                PerformedByUserId = _tenantContext.UserId
            });
        }

        if (request.UpdateProductDefaultSellingPrice)
        {
            product.DefaultBuyingPrice = request.BuyingPrice;
            product.DefaultSellingPrice = finalSellingPrice;
            product.DefaultPricingMode = request.PricingMode;
            product.DefaultMarkupPercentage = request.MarkupPercentage;
            product.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task AdjustAsync(AdjustStockRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var product = await _db.Products
            .FirstOrDefaultAsync(x => x.Id == request.ProductId && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new AppException("Product not found", 404);

        if (product.TrackingType == TrackingType.Serialized)
        {
            await AdjustSerializedStockAsync(request, product, tenantId, ct);
            return;
        }

        var inventory = await _db.ShopInventories
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ShopId == request.ShopId && x.ProductId == request.ProductId && !x.IsDeleted, ct)
            ?? throw new AppException("Inventory record not found", 404);

        if (inventory.QuantityOnHand + request.QuantityChange < 0)
            throw new AppException("Adjustment would make stock negative");

        inventory.QuantityOnHand += request.QuantityChange;
        inventory.UpdatedAt = DateTimeOffset.UtcNow;

        _db.StockMovements.Add(new StockMovement
        {
            TenantId = tenantId,
            ShopId = request.ShopId,
            ProductId = request.ProductId,
            MovementType = request.QuantityChange >= 0 ? StockMovementType.AdjustmentPositive : StockMovementType.AdjustmentNegative,
            Quantity = Math.Abs(request.QuantityChange),
            Note = request.Reason,
            PerformedByUserId = _tenantContext.UserId
        });

        await EnsureLowStockNotificationAsync(tenantId, inventory, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Guid> CreateTransferAsync(CreateStockTransferRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var transfer = new StockTransfer
        {
            TenantId = tenantId,
            SourceShopId = request.SourceShopId,
            DestinationShopId = request.DestinationShopId,
            Notes = request.Notes,
            CreatedByUserId = _tenantContext.UserId,
            Items = request.Items.Select(x => new StockTransferItem
            {
                ProductId = x.ProductId,
                Quantity = x.Quantity
            }).ToList()
        };

        _db.StockTransfers.Add(transfer);
        await _db.SaveChangesAsync(ct);
        return transfer.Id;
    }

    public async Task DispatchTransferAsync(Guid transferId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var transfer = await _db.StockTransfers
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == transferId && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new AppException("Transfer not found", 404);

        if (transfer.Status != StockTransferStatus.Draft)
            throw new AppException("Only draft transfers can be dispatched");

        foreach (var item in transfer.Items)
        {
            var stock = await _db.ShopInventories
                .Include(x => x.Product)
                .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ShopId == transfer.SourceShopId && x.ProductId == item.ProductId && !x.IsDeleted, ct)
                ?? throw new AppException($"Source stock not found for product {item.ProductId}");

            if (stock.Product!.TrackingType == TrackingType.Serialized)
                throw new AppException("Serialized transfer dispatch is not implemented in this starter");

            if (stock.QuantityOnHand < item.Quantity)
                throw new AppException("Insufficient stock for transfer");

            stock.QuantityOnHand -= item.Quantity;
            stock.UpdatedAt = DateTimeOffset.UtcNow;

            _db.StockMovements.Add(new StockMovement
            {
                TenantId = tenantId,
                ShopId = transfer.SourceShopId,
                ProductId = item.ProductId,
                MovementType = StockMovementType.TransferOut,
                Quantity = item.Quantity,
                ReferenceType = nameof(StockTransfer),
                ReferenceId = transfer.Id,
                PerformedByUserId = _tenantContext.UserId
            });

            await EnsureLowStockNotificationAsync(tenantId, stock, ct);
        }

        transfer.Status = StockTransferStatus.Dispatched;
        transfer.DispatchedByUserId = _tenantContext.UserId;
        transfer.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            Type = NotificationType.TransferDispatched,
            Title = "Stock Transfer Dispatched",
            Message = $"Stock transfer {transfer.Id} has been dispatched to shop {transfer.DestinationShopId}.",
            RelatedEntityId = transfer.Id
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task ReceiveTransferAsync(Guid transferId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var transfer = await _db.StockTransfers
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == transferId && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new AppException("Transfer not found", 404);

        if (transfer.Status != StockTransferStatus.Dispatched)
            throw new AppException("Only dispatched transfers can be received");

        foreach (var item in transfer.Items)
        {
            var stock = await _db.ShopInventories.FirstOrDefaultAsync(x =>
                x.TenantId == tenantId && x.ShopId == transfer.DestinationShopId && x.ProductId == item.ProductId && !x.IsDeleted, ct);

            if (stock is null)
            {
                var product = await _db.Products.FirstAsync(x => x.Id == item.ProductId && x.TenantId == tenantId && !x.IsDeleted, ct);
                stock = new ShopInventory
                {
                    TenantId = tenantId,
                    ShopId = transfer.DestinationShopId,
                    ProductId = item.ProductId,
                    QuantityOnHand = 0,
                    LowStockThreshold = product.LowStockThreshold
                };
                _db.ShopInventories.Add(stock);
            }

            stock.QuantityOnHand += item.Quantity;
            stock.UpdatedAt = DateTimeOffset.UtcNow;

            _db.StockMovements.Add(new StockMovement
            {
                TenantId = tenantId,
                ShopId = transfer.DestinationShopId,
                ProductId = item.ProductId,
                MovementType = StockMovementType.TransferIn,
                Quantity = item.Quantity,
                ReferenceType = nameof(StockTransfer),
                ReferenceId = transfer.Id,
                PerformedByUserId = _tenantContext.UserId
            });
        }

        transfer.Status = StockTransferStatus.Received;
        transfer.ReceivedByUserId = _tenantContext.UserId;
        transfer.UpdatedAt = DateTimeOffset.UtcNow;

        _db.Notifications.Add(new Notification
        {
            TenantId = tenantId,
            Type = NotificationType.TransferReceived,
            Title = "Stock Transfer Received",
            Message = $"Stock transfer {transfer.Id} has been received at shop {transfer.DestinationShopId}.",
            RelatedEntityId = transfer.Id
        });

        await _db.SaveChangesAsync(ct);
    }

    private async Task AdjustSerializedStockAsync(AdjustStockRequestDto request, Product product, Guid tenantId, CancellationToken ct)
    {
        if (request.QuantityChange == 0)
            throw new AppException("Quantity change must not be zero");

        ValidateSerializedUnits(request.SerializedUnits, Math.Abs(request.QuantityChange), requireExactQuantityMatch: true);

        if (request.QuantityChange > 0)
        {
            foreach (var unit in request.SerializedUnits)
            {
                var serializedUnit = new SerializedInventoryUnit
                {
                    TenantId = tenantId,
                    ShopId = request.ShopId,
                    ProductId = request.ProductId,
                    UnitBarcode = unit.UnitBarcode.Trim(),
                    SerialNumber = unit.SerialNumber?.Trim(),
                    Imei1 = unit.Imei1?.Trim(),
                    Imei2 = unit.Imei2?.Trim(),
                    PurchaseCost = product.DefaultBuyingPrice,
                    SalePrice = unit.SalePrice ?? product.DefaultSellingPrice
                };

                _db.SerializedInventoryUnits.Add(serializedUnit);
                _db.StockMovements.Add(new StockMovement
                {
                    TenantId = tenantId,
                    ShopId = request.ShopId,
                    ProductId = request.ProductId,
                    SerializedInventoryUnitId = serializedUnit.Id,
                    MovementType = StockMovementType.AdjustmentPositive,
                    Quantity = 1,
                    Note = request.Reason,
                    PerformedByUserId = _tenantContext.UserId
                });
            }
        }
        else
        {
            var selectedUnitKeys = request.SerializedUnits
                .Select(x => new SerializedUnitLookupKey(x.UnitBarcode, x.Imei1, x.Imei2))
                .ToList();

            var candidates = await _db.SerializedInventoryUnits
                .Where(x => x.TenantId == tenantId &&
                            x.ShopId == request.ShopId &&
                            x.ProductId == request.ProductId &&
                            x.Status == SerializedUnitStatus.InStock &&
                            !x.IsDeleted)
                .ToListAsync(ct);

            var unitsToRemove = new List<SerializedInventoryUnit>();
            foreach (var key in selectedUnitKeys)
            {
                var match = candidates.FirstOrDefault(x => SerializedUnitMatches(x, key));
                if (match is null)
                    throw new AppException($"Serialized unit not found for barcode '{key.UnitBarcode}'");

                unitsToRemove.Add(match);
                candidates.Remove(match);
            }

            foreach (var unit in unitsToRemove)
            {
                unit.IsDeleted = true;
                unit.UpdatedAt = DateTimeOffset.UtcNow;

                _db.StockMovements.Add(new StockMovement
                {
                    TenantId = tenantId,
                    ShopId = request.ShopId,
                    ProductId = request.ProductId,
                    SerializedInventoryUnitId = unit.Id,
                    MovementType = StockMovementType.AdjustmentNegative,
                    Quantity = 1,
                    Note = request.Reason,
                    PerformedByUserId = _tenantContext.UserId
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private static void ValidateSerializedUnits(List<SerializedStockInUnitRequestDto> serializedUnits, int expectedQuantity, bool requireExactQuantityMatch)
    {
        if (serializedUnits.Count == 0)
            throw new AppException("Serialized units are required for serialized products");

        if (requireExactQuantityMatch && serializedUnits.Count != expectedQuantity)
            throw new AppException($"Serialized units count must match quantity {expectedQuantity}");

        if (serializedUnits.Any(x => string.IsNullOrWhiteSpace(x.UnitBarcode)))
            throw new AppException("Each serialized unit must include a barcode");
    }

    private static bool SerializedUnitMatches(SerializedInventoryUnit unit, SerializedUnitLookupKey key)
    {
        return string.Equals(unit.UnitBarcode, key.UnitBarcode, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(unit.Imei1 ?? string.Empty, key.Imei1, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(unit.Imei2 ?? string.Empty, key.Imei2, StringComparison.OrdinalIgnoreCase);
    }

    private async Task EnsureLowStockNotificationAsync(Guid tenantId, ShopInventory inventory, CancellationToken ct)
    {
        if (inventory.QuantityOnHand <= inventory.LowStockThreshold)
        {
            var exists = await _db.Notifications.AnyAsync(x =>
                x.TenantId == tenantId &&
                x.Type == NotificationType.LowStock &&
                x.RelatedEntityId == inventory.ProductId &&
                !x.IsDeleted, ct);

            if (!exists)
            {
                _db.Notifications.Add(new Notification
                {
                    TenantId = tenantId,
                    Type = NotificationType.LowStock,
                    Title = "Low stock alert",
                    Message = $"Product {inventory.ProductId} is below threshold at shop {inventory.ShopId}.",
                    RelatedEntityId = inventory.ProductId
                });
            }
        }
    }

    private sealed class SerializedUnitLookupKey
    {
        public SerializedUnitLookupKey(string unitBarcode, string? imei1, string? imei2)
        {
            UnitBarcode = unitBarcode.Trim();
            Imei1 = imei1?.Trim() ?? string.Empty;
            Imei2 = imei2?.Trim() ?? string.Empty;
        }

        public string UnitBarcode { get; }
        public string Imei1 { get; }
        public string Imei2 { get; }
    }
}
