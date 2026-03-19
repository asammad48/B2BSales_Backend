using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Products;

public class CreateProductRequestDto
{
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? ModelId { get; set; }
    public Guid? PartTypeId { get; set; }
    public string Sku { get; set; } = default!;
    public string? Barcode { get; set; }
    public string Name { get; set; } = default!;
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public string? Specifications { get; set; }
    public TrackingType TrackingType { get; set; }
    public QualityType QualityType { get; set; }
    public decimal DefaultBuyingPrice { get; set; }
    public decimal DefaultSellingPrice { get; set; }
    public PricingMode DefaultPricingMode { get; set; }
    public decimal? DefaultMarkupPercentage { get; set; }
    public int WarrantyDays { get; set; }
    public int LowStockThreshold { get; set; }
    public List<CreateProductImageRequestDto>? Images { get; set; }
}
