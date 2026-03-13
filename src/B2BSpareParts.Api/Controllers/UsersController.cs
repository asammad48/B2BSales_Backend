using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.DTOs.Users.UserProfileResponseDto>.Ok(await _userService.GetMeAsync(ct)));

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Users.UserListItemResponseDto>>.Ok(await _userService.GetPagedAsync(request, ct)));
}
