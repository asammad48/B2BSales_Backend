namespace B2BSpareParts.Application.DTOs.Lookups;

public class ProductLookupResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public string? BrandName { get; set; }
    public string? ModelName { get; set; }
    public string? Barcode { get; set; }
    public bool IsActive { get; set; }
}
