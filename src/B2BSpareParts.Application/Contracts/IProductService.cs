using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.DTOs.Products;

namespace B2BSpareParts.Application.Contracts;

public interface IProductService
{
    Task<PageResponse<ProductListItemResponseDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default);
    Task<ProductDetailResponseDto> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Guid> CreateAsync(CreateProductRequestDto request, CancellationToken ct = default);
    Task<ProductPricingAdjustmentResultDto> AdjustPricingAsync(Guid productId, AdjustProductPricingRequestDto request, CancellationToken ct = default);
    Task UpdateFlagsAsync(Guid productId, UpdateProductFlagsRequestDto request, CancellationToken ct = default);
}
