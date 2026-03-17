namespace B2BSpareParts.Application.DTOs.Reports;

public class TopSellingProductReportItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public string BrandName { get; set; } = default!;
    public string ModelName { get; set; } = default!;
    public int QuantitySold { get; set; }
    public decimal TotalSales { get; set; }
}
