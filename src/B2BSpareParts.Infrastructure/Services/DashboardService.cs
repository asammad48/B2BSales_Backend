using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs;
using B2BSpareParts.Application.DTOs.Dashboard;
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
}
