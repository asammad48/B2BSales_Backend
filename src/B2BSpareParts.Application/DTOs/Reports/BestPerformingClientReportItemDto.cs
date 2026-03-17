namespace B2BSpareParts.Application.DTOs.Reports;

public class BestPerformingClientReportItemDto
{
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = default!;
    public string BusinessName { get; set; } = default!;
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal TotalSales { get; set; }
    public decimal AverageOrderValue { get; set; }
}
