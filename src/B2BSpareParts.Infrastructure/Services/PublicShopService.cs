using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.PublicShops;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class PublicShopService : IPublicShopService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public PublicShopService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<IEnumerable<PublicShopLookupItemDto>> GetShopsAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await _db.Shops
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            .OrderByDescending(x => x.IsMain)
            .ThenBy(x => x.Name)
            .Select(x => new PublicShopLookupItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Address = x.Address,
                Phone = x.Phone,
                IsMain = x.IsMain,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<B2BSpareParts.Application.DTOs.Shops.ShopLookupItemDto>> GetLookupAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        return await _db.Shops
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .OrderByDescending(x => x.IsMain)
            .ThenBy(x => x.Name)
            .Select(x => new B2BSpareParts.Application.DTOs.Shops.ShopLookupItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                IsMain = x.IsMain,
                IsActive = x.IsActive
            })
            .ToListAsync(ct);
    }
}
