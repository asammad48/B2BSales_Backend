using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs;
using B2BSpareParts.Application.DTOs.Reports;
using B2BSpareParts.Domain.Enums;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IDateRangeService _dateRangeService;

    public ReportService(AppDbContext db, ITenantContext tenantContext, IDateRangeService dateRangeService)
    {
        _db = db;
        _tenantContext = tenantContext;
        _dateRangeService = dateRangeService;
    }

    public async Task<PageResponse<BestPerformingClientReportItemDto>> GetBestPerformingClientsAsync(RangeType rangeType, DateTime? startDate, DateTime? endDate, PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        (DateTime start, DateTime end) = _dateRangeService.CalculateDateRange(rangeType, startDate, endDate);

        var query = _db.Clients
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .Select(c => new BestPerformingClientReportItemDto
            {
                ClientId = c.Id,
                ClientName = c.Name,
                BusinessName = c.BusinessName,
                TotalOrders = _db.Orders.Count(o => o.ClientId == c.Id && !o.IsDeleted && o.CreatedAt >= start && o.CreatedAt <= end),
                CompletedOrders = _db.Orders.Count(o => o.ClientId == c.Id && !o.IsDeleted && o.Status == OrderStatus.Completed && o.CreatedAt >= start && o.CreatedAt <= end),
                TotalSales = _db.Orders.Where(o => o.ClientId == c.Id && !o.IsDeleted && o.Status == OrderStatus.Completed && o.CreatedAt >= start && o.CreatedAt <= end).Sum(o => o.TotalAmount),
            })
            .Where(x => x.TotalOrders > 0)
            .OrderByDescending(x => x.TotalSales);

        var result = await query.ToPageAsync(request, ct);

        foreach (var item in result.Items)
        {
            if (item.CompletedOrders > 0)
            {
                item.AverageOrderValue = item.TotalSales / item.CompletedOrders;
            }
        }

        return result;
    }

    public async Task<PageResponse<TopSellingProductReportItemDto>> GetTopSellingProductsAsync(RangeType rangeType, DateTime? startDate, DateTime? endDate, PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        (DateTime start, DateTime end) = _dateRangeService.CalculateDateRange(rangeType, startDate, endDate);

        var query = _db.OrderItems
            .Where(oi => oi.Order!.TenantId == tenantId && !oi.Order.IsDeleted && oi.Order.Status == OrderStatus.Completed && oi.Order.CreatedAt >= start && oi.Order.CreatedAt <= end)
            .GroupBy(oi => oi.ProductId)
            .Select(g => new TopSellingProductReportItemDto
            {
                ProductId = g.Key,
                ProductName = g.First().Product!.Name,
                Sku = g.First().Product!.Sku,
                BrandName = g.First().Product!.Brand!.Name,
                ModelName = g.First().Product!.Model!.Name,
                QuantitySold = g.Sum(oi => oi.Quantity),
                TotalSales = g.Sum(oi => oi.LineTotal)
            })
            .OrderByDescending(x => x.QuantitySold);

        return await query.ToPageAsync(request, ct);
    }

    public async Task<PageResponse<LowStockReportItemDto>> GetLowStockProductsAsync(PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var query = _db.ShopInventories
            .Where(si => si.TenantId == tenantId && !si.IsDeleted && si.QuantityOnHand <= si.LowStockThreshold)
            .Select(si => new LowStockReportItemDto
            {
                ProductId = si.ProductId,
                ProductName = si.Product!.Name,
                Sku = si.Product!.Sku,
                ShopId = si.ShopId,
                ShopName = si.Shop!.Name,
                StockQuantity = si.QuantityOnHand,
                LowStockThreshold = si.LowStockThreshold
            })
            .OrderBy(x => x.StockQuantity);

        return await query.ToPageAsync(request, ct);
    }

    public async Task<List<SalesByShopReportItemDto>> GetSalesByShopAsync(RangeType rangeType, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        (DateTime start, DateTime end) = _dateRangeService.CalculateDateRange(rangeType, startDate, endDate);

        var result = await _db.Shops
            .Where(s => s.TenantId == tenantId && !s.IsDeleted)
            .Select(s => new SalesByShopReportItemDto
            {
                ShopId = s.Id,
                ShopName = s.Name,
                TotalSales = _db.Orders.Where(o => o.ShopId == s.Id && !o.IsDeleted && o.Status == OrderStatus.Completed && o.CreatedAt >= start && o.CreatedAt <= end).Sum(o => o.TotalAmount),
                CompletedOrders = _db.Orders.Count(o => o.ShopId == s.Id && !o.IsDeleted && o.Status == OrderStatus.Completed && o.CreatedAt >= start && o.CreatedAt <= end),
            })
            .ToListAsync(ct);

        foreach (var item in result)
        {
            if (item.CompletedOrders > 0)
            {
                item.AverageOrderValue = item.TotalSales / item.CompletedOrders;
            }
        }

        return result;
    }

    public async Task<OrderStatusSummaryDto> GetOrderStatusSummaryAsync(RangeType rangeType, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        (DateTime start, DateTime end) = _dateRangeService.CalculateDateRange(rangeType, startDate, endDate);

        var orders = await _db.Orders
            .Where(o => o.TenantId == tenantId && !o.IsDeleted && o.CreatedAt >= start && o.CreatedAt <= end)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return new OrderStatusSummaryDto
        {
            PendingOrders = orders.FirstOrDefault(x => x.Status == OrderStatus.Pending)?.Count ?? 0,
            ReadyForPickupOrders = orders.FirstOrDefault(x => x.Status == OrderStatus.ReadyForPickup)?.Count ?? 0,
            CompletedOrders = orders.FirstOrDefault(x => x.Status == OrderStatus.Completed)?.Count ?? 0,
            CancelledOrders = orders.FirstOrDefault(x => x.Status == OrderStatus.Cancelled)?.Count ?? 0,
            UnableToFulfillOrders = orders.FirstOrDefault(x => x.Status == OrderStatus.UnableToFulfill)?.Count ?? 0,
        };
    }
}
