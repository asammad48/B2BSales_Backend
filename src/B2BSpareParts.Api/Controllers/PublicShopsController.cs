using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.PublicShops;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public")]
public class PublicShopsController : ControllerBase
{
    private readonly IPublicShopService _publicShopService;

    public PublicShopsController(IPublicShopService publicShopService)
    {
        _publicShopService = publicShopService;
    }

    [HttpGet("shops")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PublicShopLookupItemDto>>>> GetShops(CancellationToken ct)
        => Ok(ApiResponse<IEnumerable<PublicShopLookupItemDto>>.Ok(await _publicShopService.GetShopsAsync(ct)));
}
