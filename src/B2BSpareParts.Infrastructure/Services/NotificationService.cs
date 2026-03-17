using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Notifications;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public NotificationService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PageResponse<NotificationListItemResponseDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _db.Notifications.AsNoTracking().Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(search) || x.Message.ToLower().Contains(search));
        }

        var projected = query
            .ApplyCreatedAtSort(request)
            .Select(x => new NotificationListItemResponseDto
            {
                Id = x.Id,
                Type = x.Type.ToString(),
                Title = x.Title,
                Message = x.Message,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt,
                ReferenceId = x.RelatedEntityId,
                ReferenceType = x.Type == B2BSpareParts.Domain.Enums.NotificationType.NewOrder || x.Type == B2BSpareParts.Domain.Enums.NotificationType.OrderReady || x.Type == B2BSpareParts.Domain.Enums.NotificationType.OrderUnableToFulfill ? "Order" :
                                x.Type == B2BSpareParts.Domain.Enums.NotificationType.LowStock ? "Product" :
                                x.Type == B2BSpareParts.Domain.Enums.NotificationType.TransferDispatched || x.Type == B2BSpareParts.Domain.Enums.NotificationType.TransferReceived ? "StockTransfer" : null
            });

        return await projected.ToPageAsync(request, ct);
    }

    public async Task MarkAsReadAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var notification = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new AppException("Notification not found", 404);

        notification.IsRead = true;
        notification.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
