using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.DTOs;
using B2BSpareParts.Application.DTOs.Reports;

namespace B2BSpareParts.Application.Contracts;

public interface IReportService
{
    Task<PageResponse<BestPerformingClientReportItemDto>> GetBestPerformingClientsAsync(RangeType rangeType, DateTime? startDate, DateTime? endDate, PageRequest request, CancellationToken ct = default);
    Task<PageResponse<TopSellingProductReportItemDto>> GetTopSellingProductsAsync(RangeType rangeType, DateTime? startDate, DateTime? endDate, PageRequest request, CancellationToken ct = default);
    Task<PageResponse<LowStockReportItemDto>> GetLowStockProductsAsync(PageRequest request, CancellationToken ct = default);
    Task<List<SalesByShopReportItemDto>> GetSalesByShopAsync(RangeType rangeType, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
    Task<OrderStatusSummaryDto> GetOrderStatusSummaryAsync(RangeType rangeType, DateTime? startDate, DateTime? endDate, CancellationToken ct = default);
}
