using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Products;

public class ProductListItemResponseDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = default!;
    public string? Barcode { get; set; }
    public string Name { get; set; } = default!;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = default!;
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public Guid? ModelId { get; set; }
    public string? ModelName { get; set; }
    public Guid? PartTypeId { get; set; }
    public string? PartTypeName { get; set; }
    public TrackingType TrackingType { get; set; }
    public QualityType QualityType { get; set; }
    public decimal? DefaultSellingPrice { get; set; }
    public string? PrimaryImageUrl { get; set; }
    public int QuantityInHand { get; set; }
    public bool IsActive { get; set; }
    public bool IsPriceLocked { get; set; }
    public bool CanOrder { get; set; }
}
