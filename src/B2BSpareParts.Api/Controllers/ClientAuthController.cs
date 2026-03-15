using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/client-auth")]
public class ClientAuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public ClientAuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<ClientLoginResponseDto>>> Login([FromBody] ClientLoginRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<ClientLoginResponseDto>.Ok(await _authService.ClientLoginAsync(request, ct)));
}
