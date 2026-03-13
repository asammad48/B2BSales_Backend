using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.DTOs.Products;

namespace B2BSpareParts.Application.Contracts;

public interface IProductService
{
    Task<PageResponse<ProductListItemResponseDto>> GetPagedAsync(PageRequest request, bool isGuestView, CancellationToken ct = default);
    Task<ProductDetailResponseDto> GetByIdAsync(Guid id, bool isGuestView, CancellationToken ct = default);
    Task<Guid> CreateAsync(CreateProductRequestDto request, CancellationToken ct = default);
}
