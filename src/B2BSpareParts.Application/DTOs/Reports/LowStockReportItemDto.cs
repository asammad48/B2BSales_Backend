namespace B2BSpareParts.Application.DTOs.Reports;

public class LowStockReportItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = default!;
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; }
}
