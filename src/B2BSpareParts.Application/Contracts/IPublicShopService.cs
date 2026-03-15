using B2BSpareParts.Application.DTOs.PublicShops;

namespace B2BSpareParts.Application.Contracts;

public interface IPublicShopService
{
    Task<IEnumerable<PublicShopLookupItemDto>> GetShopsByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
}
