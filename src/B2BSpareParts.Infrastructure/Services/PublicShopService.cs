using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.PublicShops;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class PublicShopService : IPublicShopService
{
    private readonly AppDbContext _db;

    public PublicShopService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<PublicShopLookupItemDto>> GetShopsByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
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
}
