using B2BSpareParts.Application.DTOs.PublicShops;

namespace B2BSpareParts.Application.Contracts;

public interface IPublicShopService
{
    Task<IEnumerable<PublicShopLookupItemDto>> GetShopsAsync(CancellationToken ct = default);
    Task<IEnumerable<B2BSpareParts.Application.DTOs.Shops.ShopLookupItemDto>> GetLookupAsync(CancellationToken ct = default);
}
