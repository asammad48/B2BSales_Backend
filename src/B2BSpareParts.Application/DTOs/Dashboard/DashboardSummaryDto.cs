namespace B2BSpareParts.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalClients { get; set; }
    public decimal TotalSales { get; set; }
    public int ActiveOrders { get; set; }
    public RangeType RangeType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}
