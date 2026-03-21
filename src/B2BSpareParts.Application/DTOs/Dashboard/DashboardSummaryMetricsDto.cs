using B2BSpareParts.Application.DTOs.Reports;

namespace B2BSpareParts.Application.DTOs.Dashboard;

public class DashboardSummaryMetricsDto
{
    public int TotalClients { get; set; }
    public decimal TotalSales { get; set; }
    public int ActiveOrders { get; set; }
    public int TotalProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int PendingOrders { get; set; }
    public int ReadyForPickupOrders { get; set; }
    public int CompletedOrders { get; set; }
    public OrderStatusSummaryDto OrderStatusSummary { get; set; } = new();
    public int UnreadInquiriesCount { get; set; }
}
