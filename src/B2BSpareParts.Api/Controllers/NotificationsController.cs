using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]

[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] PageRequest request, CancellationToken ct)
        => Ok(ApiResponse<B2BSpareParts.Application.Common.PageResponse<B2BSpareParts.Application.DTOs.Notifications.NotificationListItemResponseDto>>.Ok(await _notificationService.GetPagedAsync(request, ct)));
}
