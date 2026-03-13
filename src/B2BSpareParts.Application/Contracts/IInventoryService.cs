using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.DTOs.Inventory;

namespace B2BSpareParts.Application.Contracts;

public interface IInventoryService
{
    Task<PageResponse<InventoryListItemResponseDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default);
    Task StockInAsync(StockInRequestDto request, CancellationToken ct = default);
    Task AdjustAsync(AdjustStockRequestDto request, CancellationToken ct = default);
    Task<Guid> CreateTransferAsync(CreateStockTransferRequestDto request, CancellationToken ct = default);
    Task DispatchTransferAsync(Guid transferId, CancellationToken ct = default);
    Task ReceiveTransferAsync(Guid transferId, CancellationToken ct = default);
}
