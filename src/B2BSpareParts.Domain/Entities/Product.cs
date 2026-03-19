using B2BSpareParts.Domain.Common;
using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Domain.Entities;

public class Product : TenantEntity
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
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public bool IsPublicVisible { get; set; }
    public bool IsFeatured { get; set; }
    public int SortOrder { get; set; }

    public Tenant? Tenant { get; set; }
    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public DeviceModel? Model { get; set; }
    public PartType? PartType { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
