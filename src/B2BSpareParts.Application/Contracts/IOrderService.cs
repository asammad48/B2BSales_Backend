using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.DTOs.Orders;

namespace B2BSpareParts.Application.Contracts;

public interface IOrderService
{
    Task<PageResponse<OrderListItemResponseDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default);
    Task<Guid> CreateAsync(CreateOrderRequestDto request, CancellationToken ct = default);
    Task MarkReadyAsync(Guid id, CancellationToken ct = default);
    Task CompleteAsync(Guid id, CancellationToken ct = default);
    Task MarkUnableToFulfillAsync(Guid id, string? reason = null, CancellationToken ct = default);
}
