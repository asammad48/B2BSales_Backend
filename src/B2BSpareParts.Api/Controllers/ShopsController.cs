using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Shops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShopsController : ControllerBase
{
    private readonly IPublicShopService _shopService;

    public ShopsController(IPublicShopService shopService)
    {
        _shopService = shopService;
    }

    [HttpGet("lookup")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ShopLookupItemDto>>>> GetLookup(CancellationToken ct)
        => Ok(ApiResponse<IEnumerable<ShopLookupItemDto>>.Ok(await _shopService.GetLookupAsync(ct)));
}
