using B2BSpareParts.Application.Common;

namespace B2BSpareParts.Application.DTOs.PublicCatalog;

public class GetPublicProductsRequestDto : PageRequest
{
    public new string? Search { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? ModelId { get; set; }
    public Guid? PartTypeId { get; set; }
}
