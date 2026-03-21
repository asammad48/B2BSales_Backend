using B2BSpareParts.Application.DTOs.Orders;
using B2BSpareParts.Application.DTOs.Reports;

namespace B2BSpareParts.Application.DTOs.Dashboard;

public class DashboardOverviewDto
{
    public DashboardSummaryMetricsDto Summary { get; set; } = new();
    public List<OrderListItemResponseDto> RecentOrders { get; set; } = new();
    public List<InquiryPreviewDto> RecentInquiries { get; set; } = new();
    public List<LowStockReportItemDto> LowStockPreview { get; set; } = new();
    public List<BestPerformingClientReportItemDto> BestPerformingClientsPreview { get; set; } = new();
    public List<TopSellingProductReportItemDto> TopSellingProductsPreview { get; set; } = new();
    public int UnreadNotificationsCount { get; set; }
}
