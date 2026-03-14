namespace B2BSpareParts.Application.DTOs.PublicCatalog;

public class PublicLookupItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public int ProductCount { get; set; }
}
