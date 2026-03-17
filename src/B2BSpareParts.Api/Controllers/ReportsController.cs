using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs;
using B2BSpareParts.Application.DTOs.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("clients/best-performing")]
    public async Task<ActionResult<ApiResponse<PageResponse<BestPerformingClientReportItemDto>>>> GetBestPerformingClients(
        [FromQuery] DateRangeReportRequestDto dateRange,
        [FromQuery] PageRequest request,
        CancellationToken ct)
    {
        return Ok(ApiResponse<PageResponse<BestPerformingClientReportItemDto>>.Ok(await _reportService.GetBestPerformingClientsAsync(dateRange.RangeType, dateRange.StartDate, dateRange.EndDate, request, ct)));
    }

    [HttpGet("products/top-selling")]
    public async Task<ActionResult<ApiResponse<PageResponse<TopSellingProductReportItemDto>>>> GetTopSellingProducts(
        [FromQuery] DateRangeReportRequestDto dateRange,
        [FromQuery] PageRequest request,
        CancellationToken ct)
    {
        return Ok(ApiResponse<PageResponse<TopSellingProductReportItemDto>>.Ok(await _reportService.GetTopSellingProductsAsync(dateRange.RangeType, dateRange.StartDate, dateRange.EndDate, request, ct)));
    }

    [HttpGet("products/low-stock")]
    public async Task<ActionResult<ApiResponse<PageResponse<LowStockReportItemDto>>>> GetLowStockProducts(
        [FromQuery] PageRequest request,
        CancellationToken ct)
    {
        return Ok(ApiResponse<PageResponse<LowStockReportItemDto>>.Ok(await _reportService.GetLowStockProductsAsync(request, ct)));
    }

    [HttpGet("sales/by-shop")]
    public async Task<ActionResult<ApiResponse<List<SalesByShopReportItemDto>>>> GetSalesByShop(
        [FromQuery] DateRangeReportRequestDto dateRange,
        CancellationToken ct)
    {
        return Ok(ApiResponse<List<SalesByShopReportItemDto>>.Ok(await _reportService.GetSalesByShopAsync(dateRange.RangeType, dateRange.StartDate, dateRange.EndDate, ct)));
    }

    [HttpGet("orders/status-summary")]
    public async Task<ActionResult<ApiResponse<OrderStatusSummaryDto>>> GetOrderStatusSummary(
        [FromQuery] DateRangeReportRequestDto dateRange,
        CancellationToken ct)
    {
        return Ok(ApiResponse<OrderStatusSummaryDto>.Ok(await _reportService.GetOrderStatusSummaryAsync(dateRange.RangeType, dateRange.StartDate, dateRange.EndDate, ct)));
    }
}
