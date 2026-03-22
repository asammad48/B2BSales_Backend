using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.DTOs.Orders.Invoices;
using B2BSpareParts.Application.DTOs.Pos;

namespace B2BSpareParts.Application.Contracts;

public interface IPosService
{
    Task<PageResponse<PosProductListItemDto>> GetProductsAsync(GetPosProductsRequestDto request, CancellationToken ct = default);
    Task<CreatePosOrderResponseDto> CreateOrderAsync(CreatePosOrderRequestDto request, CancellationToken ct = default);
    Task<OrderInvoicePdfDto> GetInvoicePdfAsync(Guid orderId, CancellationToken ct = default);
}
