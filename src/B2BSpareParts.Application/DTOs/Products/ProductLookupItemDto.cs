using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Products;

public class ProductLookupItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public string? BrandName { get; set; }
    public string? ModelName { get; set; }
    public TrackingType TrackingType { get; set; }
}
