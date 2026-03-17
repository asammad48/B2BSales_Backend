using B2BSpareParts.Application.DTOs.Dashboard;
using B2BSpareParts.Application.DTOs;

namespace B2BSpareParts.Application.Contracts;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(RangeType rangeType, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
}
