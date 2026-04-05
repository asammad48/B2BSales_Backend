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

    [HttpGet("tenants/{tenantId:guid}/shops")]
    public async Task<ActionResult<ApiResponse<IEnumerable<PublicShopLookupItemDto>>>> GetShopsByTenantId(Guid tenantId, CancellationToken ct)
        => Ok(ApiResponse<IEnumerable<PublicShopLookupItemDto>>.Ok(await _publicShopService.GetShopsByTenantIdAsync(tenantId, ct)));

    [HttpGet("tenant/{tenantId:guid}/client-info")]
    public async Task<ActionResult<ApiResponse<PublicTenantClientInfoResponseDto>>> GetTenantClientInfo(Guid tenantId, CancellationToken ct)
        => Ok(ApiResponse<PublicTenantClientInfoResponseDto>.Ok(await _publicShopService.GetTenantClientInfoByTenantIdAsync(tenantId, ct)));
}
