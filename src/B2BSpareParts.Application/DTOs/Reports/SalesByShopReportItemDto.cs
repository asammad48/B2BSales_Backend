namespace B2BSpareParts.Application.DTOs.Reports;

public class SalesByShopReportItemDto
{
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = default!;
    public decimal TotalSales { get; set; }
    public int CompletedOrders { get; set; }
    public decimal AverageOrderValue { get; set; }
}
