using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.DTOs.Notifications;

namespace B2BSpareParts.Application.Contracts;

public interface INotificationService
{
    Task<PageResponse<NotificationListItemResponseDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default);
    Task MarkAsReadAsync(Guid id, CancellationToken ct = default);
}
