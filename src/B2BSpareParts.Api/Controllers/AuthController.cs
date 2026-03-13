using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.DTOs.Auth.LoginResponseDto>>> Login([FromBody] B2BSpareParts.Application.DTOs.Auth.LoginRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.DTOs.Auth.LoginResponseDto>.Ok(await _authService.LoginAsync(request, ct)));

    [Authorize]
    [HttpPost("change-password")]
    public async Task<ActionResult<ApiResponse<string>>> ChangePassword([FromBody] B2BSpareParts.Application.DTOs.Auth.ChangePasswordRequestDto request, CancellationToken ct)
    {
        await _authService.ChangePasswordAsync(request, ct);
        return Ok(ApiResponse<string>.Ok("Password changed"));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<B2BSpareParts.Application.DTOs.Auth.ForgotPasswordResponseDto>>> ForgotPassword([FromBody] B2BSpareParts.Application.DTOs.Auth.ForgotPasswordRequestDto request, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.DTOs.Auth.ForgotPasswordResponseDto>.Ok(await _authService.ForgotPasswordAsync(request, ct)));

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword([FromBody] B2BSpareParts.Application.DTOs.Auth.ResetPasswordRequestDto request, CancellationToken ct)
    {
        await _authService.ResetPasswordAsync(request, ct);
        return Ok(ApiResponse<string>.Ok("Password reset"));
    }
}
