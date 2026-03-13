using B2BSpareParts.Domain.Common;
using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Domain.Entities;

public class Notification : TenantEntity
{
    public NotificationType Type { get; set; }
    public string Title { get; set; } = default!;
    public string Message { get; set; } = default!;
    public bool IsRead { get; set; }
    public Guid? RelatedEntityId { get; set; }
}
