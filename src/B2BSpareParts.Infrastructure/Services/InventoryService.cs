using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
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
        var query = _db.ShopInventories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Product!.Name.ToLower().Contains(search) ||
                x.Product.Sku.ToLower().Contains(search) ||
                (x.Product.Barcode != null && x.Product.Barcode.ToLower().Contains(search)) ||
                x.Shop!.Name.ToLower().Contains(search));
        }

        var projected = query
            .OrderBy(x => x.Product!.Name)
            .Select(x => new InventoryListItemResponseDto
            {
                ProductId = x.ProductId,
                ProductName = x.Product!.Name,
                ShopId = x.ShopId,
                ShopName = x.Shop!.Name,
                BrandId = x.Product.BrandId,
                BrandName = x.Product.Brand != null ? x.Product.Brand.Name : null,
                ModelId = x.Product.ModelId,
                ModelName = x.Product.Model != null ? x.Product.Model.Name : null,
                Sku = x.Product.Sku,
                Barcode = x.Product.Barcode,
                TrackingType = x.Product.TrackingType.ToString(),
                QuantityOnHand = x.QuantityOnHand,
                ReservedQuantity = x.ReservedQuantity,
                AvailableQuantity = x.QuantityOnHand - x.ReservedQuantity,
                LowStockThreshold = x.LowStockThreshold
            });

        return await projected.ToPageAsync(request, ct);
    }

    public async Task StockInAsync(StockInRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId && x.TenantId == tenantId && !x.IsDeleted, ct)
                      ?? throw new AppException("Product not found", 404);

        if (request.PricingMode == PricingMode.PercentageBased && request.MarkupPercentage is null)
            throw new AppException("Markup percentage is required for percentage-based pricing");

        var finalSellingPrice = request.SellingPrice ??
                                (request.PricingMode == PricingMode.PercentageBased
                                    ? request.BuyingPrice + (request.BuyingPrice * (request.MarkupPercentage ?? 0) / 100m)
                                    : product.DefaultSellingPrice);

        if (product.TrackingType == TrackingType.Serialized)
        {
            if (request.SerializedUnits.Count == 0)
                throw new AppException("Serialized units are required for serialized products");

            foreach (var unit in request.SerializedUnits)
            {
                _db.SerializedInventoryUnits.Add(new SerializedInventoryUnit
                {
                    TenantId = tenantId,
                    ShopId = request.ShopId,
                    ProductId = request.ProductId,
                    UnitBarcode = unit.UnitBarcode,
                    SerialNumber = unit.SerialNumber,
                    Imei1 = unit.Imei1,
                    Imei2 = unit.Imei2,
                    PurchaseCost = request.BuyingPrice,
                    SalePrice = unit.SalePrice ?? finalSellingPrice
                });

                _db.StockMovements.Add(new StockMovement
                {
                    TenantId = tenantId,
                    ShopId = request.ShopId,
                    ProductId = request.ProductId,
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
        var inventory = await _db.ShopInventories
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ShopId == request.ShopId && x.ProductId == request.ProductId && !x.IsDeleted, ct)
            ?? throw new AppException("Inventory record not found", 404);

        if (inventory.Product!.TrackingType == TrackingType.Serialized)
            throw new AppException("Use serialized unit workflows for serialized products");

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
        await _db.SaveChangesAsync(ct);
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
}
