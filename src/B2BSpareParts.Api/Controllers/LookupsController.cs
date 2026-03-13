using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

[Authorize]
public class LookupsController : ControllerBase
{
    private readonly ILookupService _lookupService;

    public LookupsController(ILookupService lookupService)
    {
        _lookupService = lookupService;
    }

    [HttpGet("bundle")]
    public async Task<IActionResult> GetBundle(CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.DTOs.Lookups.LookupBundleResponseDto>.Ok(await _lookupService.GetBundleAsync(ct)));
}
