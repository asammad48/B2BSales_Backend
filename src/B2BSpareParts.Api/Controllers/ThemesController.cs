using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

[Authorize]
public class ThemesController : ControllerBase
{
    private readonly IThemeService _themeService;

    public ThemesController(IThemeService themeService)
    {
        _themeService = themeService;
    }

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.DTOs.Themes.ThemeResponseDto>>> Get(CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.DTOs.Themes.ThemeResponseDto>.Ok(await _themeService.GetAsync(ct)));

    [HttpPut("current")]
    public async Task<ActionResult<ApiResponse<string>>> Update([FromBody] B2BSpareParts.Application.DTOs.Themes.UpdateThemeRequestDto request, CancellationToken ct)
    {
        await _themeService.UpdateAsync(request, ct);
        return Ok(ApiResponse<string>.Ok("Theme updated"));
    }
}
