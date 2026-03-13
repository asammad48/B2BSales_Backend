namespace B2BSpareParts.Application.DTOs.Notifications;

public class NotificationListItemResponseDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
