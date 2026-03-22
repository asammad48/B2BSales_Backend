using B2BSpareParts.Application.Common;

namespace B2BSpareParts.Application.DTOs.Pos;

public class GetPosProductsRequestDto : PageRequest
{
    public Guid? ShopId { get; set; }
}
