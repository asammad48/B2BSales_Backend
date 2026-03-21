using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs;
using B2BSpareParts.Application.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary(
        [FromQuery] DateRangeReportRequestDto request,
        CancellationToken ct)
    {
        if (request.RangeType == RangeType.Custom && (!request.StartDate.HasValue || !request.EndDate.HasValue))
        {
            return BadRequest(ApiResponse<DashboardSummaryDto>.Fail("Start and end dates are required for custom range"));
        }

        var summary = await _dashboardService.GetSummaryAsync(request.RangeType, request.StartDate, request.EndDate, ct);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(summary));
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<DashboardOverviewDto>>> GetOverview(
        [FromQuery] DateRangeReportRequestDto request,
        CancellationToken ct)
    {
        if (request.RangeType == RangeType.Custom && (!request.StartDate.HasValue || !request.EndDate.HasValue))
        {
            return BadRequest(ApiResponse<DashboardOverviewDto>.Fail("Start and end dates are required for custom range"));
        }

        var overview = await _dashboardService.GetOverviewAsync(request.RangeType, request.StartDate, request.EndDate, ct);
        return Ok(ApiResponse<DashboardOverviewDto>.Ok(overview));
    }
}
