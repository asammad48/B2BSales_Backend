using B2BSpareParts.Application.Common;
using System.ComponentModel.DataAnnotations;

namespace B2BSpareParts.Application.DTOs.PublicCatalog;

public class GetFeaturedProductsRequestDto : PageRequest
{
    public new string? Search { get; set; }

    [Required]
    public Guid? ShopId { get; set; }
}
