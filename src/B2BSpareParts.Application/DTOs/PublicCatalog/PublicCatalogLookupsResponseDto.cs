namespace B2BSpareParts.Application.DTOs.PublicCatalog;

public class PublicCatalogLookupsResponseDto
{
    public List<PublicLookupItemDto> Categories { get; set; } = [];
    public List<PublicLookupItemDto> Brands { get; set; } = [];
    public List<PublicLookupItemDto> Models { get; set; } = [];
    public List<PublicLookupItemDto> PartTypes { get; set; } = [];
}
