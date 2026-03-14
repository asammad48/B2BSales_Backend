using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.DTOs.PublicCatalog;

namespace B2BSpareParts.Application.Contracts;

public interface IPublicCatalogService
{
    Task<PublicCatalogFiltersResponseDto> GetFiltersAsync(CancellationToken ct = default);
    Task<PageResponse<PublicProductListItemDto>> GetProductsAsync(GetPublicProductsRequestDto request, CancellationToken ct = default);
}
