namespace B2BSpareParts.Application.DTOs.Pos;

public class PosProductListItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public string? Barcode { get; set; }
    public string? BrandName { get; set; }
    public string? ModelName { get; set; }
    public string? PartTypeName { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public decimal SellingPrice { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public int QuantityInHand { get; set; }
    public int LowStockThreshold { get; set; }
    public bool IsLowStock { get; set; }
}
