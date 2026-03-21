using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs;
using B2BSpareParts.Application.DTOs.Dashboard;
using B2BSpareParts.Application.DTOs.Orders;
using B2BSpareParts.Application.DTOs.Reports;
using B2BSpareParts.Domain.Enums;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IDateRangeService _dateRangeService;

    public DashboardService(AppDbContext db, ITenantContext tenantContext, IDateRangeService dateRangeService)
    {
        _db = db;
        _tenantContext = tenantContext;
        _dateRangeService = dateRangeService;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(RangeType rangeType, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        (DateTime start, DateTime end) = _dateRangeService.CalculateDateRange(rangeType, startDate, endDate);

        var totalClients = await _db.Clients
            .CountAsync(x => x.TenantId == tenantId && x.Status == ClientStatus.Approved && !x.IsDeleted, ct);

        var salesQuery = _db.Orders
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.Status == OrderStatus.Completed);

        salesQuery = salesQuery.Where(x => x.CreatedAt >= start && x.CreatedAt <= end);

        var totalSales = await salesQuery.SumAsync(x => x.TotalAmount, ct);

        var activeStatuses = new[] { OrderStatus.Pending, OrderStatus.ReadyForPickup };
        var activeOrders = await _db.Orders
            .CountAsync(x => x.TenantId == tenantId && !x.IsDeleted && activeStatuses.Contains(x.Status) && x.CreatedAt >= start && x.CreatedAt <= end, ct);

        return new DashboardSummaryDto
        {
            TotalClients = totalClients,
            TotalSales = totalSales,
            ActiveOrders = activeOrders,
            RangeType = rangeType,
            StartDate = start,
            EndDate = end
        };
    }

    public async Task<DashboardOverviewDto> GetOverviewAsync(RangeType rangeType, DateTime? startDate, DateTime? endDate, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        (DateTime start, DateTime end) = _dateRangeService.CalculateDateRange(rangeType, startDate, endDate);

        // Summary Metrics
        var totalClients = await _db.Clients
            .CountAsync(x => x.TenantId == tenantId && x.Status == ClientStatus.Approved && !x.IsDeleted, ct);

        var salesQuery = _db.Orders
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.Status == OrderStatus.Completed && x.CreatedAt >= start && x.CreatedAt <= end);
        var totalSales = await salesQuery.SumAsync(x => x.TotalAmount, ct);

        var activeStatuses = new[] { OrderStatus.Pending, OrderStatus.ReadyForPickup };
        var activeOrders = await _db.Orders
            .CountAsync(x => x.TenantId == tenantId && !x.IsDeleted && activeStatuses.Contains(x.Status) && x.CreatedAt >= start && x.CreatedAt <= end, ct);

        var totalProducts = await _db.Products
            .CountAsync(x => x.TenantId == tenantId && !x.IsDeleted, ct);

        var lowStockProducts = await _db.ShopInventories
            .Where(si => si.TenantId == tenantId && !si.IsDeleted && si.QuantityOnHand <= si.LowStockThreshold)
            .Select(si => si.ProductId)
            .Distinct()
            .CountAsync(ct);

        var orderStats = await _db.Orders
            .Where(o => o.TenantId == tenantId && !o.IsDeleted && o.CreatedAt >= start && o.CreatedAt <= end)
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var pendingOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.Pending)?.Count ?? 0;
        var readyForPickupOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.ReadyForPickup)?.Count ?? 0;
        var completedOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.Completed)?.Count ?? 0;

        // Recent Orders
        var recentOrders = await _db.Orders
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
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
            })
            .ToListAsync(ct);

        // Low Stock Preview
        var lowStockPreview = await _db.ShopInventories
            .Where(si => si.TenantId == tenantId && !si.IsDeleted && si.QuantityOnHand <= si.LowStockThreshold)
            .OrderBy(x => x.QuantityOnHand)
            .Take(5)
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
            .ToListAsync(ct);

        // Best Performing Clients
        var bestClients = await _db.Clients
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
            .OrderByDescending(x => x.TotalSales)
            .Take(5)
            .ToListAsync(ct);

        foreach (var item in bestClients)
        {
            if (item.CompletedOrders > 0)
            {
                item.AverageOrderValue = item.TotalSales / item.CompletedOrders;
            }
        }

        // Top Selling Products
        var topProducts = await _db.OrderItems
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
            .OrderByDescending(x => x.QuantitySold)
            .Take(5)
            .ToListAsync(ct);

        var unreadNotificationsCount = await _db.Notifications
            .CountAsync(x => x.TenantId == tenantId && !x.IsRead && !x.IsDeleted, ct);

        var recentInquiries = await _db.ContactInquiries
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Take(5)
            .Select(x => new InquiryPreviewDto
            {
                Id = x.Id,
                Name = x.Name,
                Subject = x.Subject,
                Status = x.Status.ToString(),
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(ct);

        var unreadInquiriesCount = await _db.ContactInquiries
            .CountAsync(x => x.TenantId == tenantId && !x.IsRead && !x.IsDeleted, ct);

        return new DashboardOverviewDto
        {
            Summary = new DashboardSummaryMetricsDto
            {
                TotalClients = totalClients,
                TotalSales = totalSales,
                ActiveOrders = activeOrders,
                TotalProducts = totalProducts,
                LowStockProducts = lowStockProducts,
                PendingOrders = pendingOrders,
                ReadyForPickupOrders = readyForPickupOrders,
                CompletedOrders = completedOrders,
                OrderStatusSummary = new OrderStatusSummaryDto
                {
                    PendingOrders = pendingOrders,
                    ReadyForPickupOrders = readyForPickupOrders,
                    CompletedOrders = completedOrders,
                    CancelledOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.Cancelled)?.Count ?? 0,
                    UnableToFulfillOrders = orderStats.FirstOrDefault(x => x.Status == OrderStatus.UnableToFulfill)?.Count ?? 0,
                },
                UnreadInquiriesCount = unreadInquiriesCount
            },
            RecentOrders = recentOrders,
            RecentInquiries = recentInquiries,
            LowStockPreview = lowStockPreview,
            BestPerformingClientsPreview = bestClients,
            TopSellingProductsPreview = topProducts,
            UnreadNotificationsCount = unreadNotificationsCount
        };
    }
}
