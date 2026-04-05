using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.Common;
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

    public async Task<PublicTenantClientInfoResponseDto> GetTenantClientInfoByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Include(x => x.DefaultSellingCurrency)
            .FirstOrDefaultAsync(x => x.Id == tenantId && x.IsActive && !x.IsDeleted, ct)
            ?? throw new AppException("Tenant not found", 404);

        var clients = await _db.Clients
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .Include(x => x.PreferredCurrency)
            .Select(x => new PublicClientInfoItemDto
            {
                ClientId = x.Id,
                Name = x.Name,
                BusinessName = x.BusinessName,
                Phone = x.Phone,
                Email = x.Email,
                Address = x.Address,
                Status = x.Status.ToString(),
                CurrencyCode = x.PreferredCurrency != null
                    ? x.PreferredCurrency.Code
                    : tenant.DefaultSellingCurrency != null ? tenant.DefaultSellingCurrency.Code : null,
                CurrencySymbol = x.PreferredCurrency != null
                    ? x.PreferredCurrency.Symbol
                    : tenant.DefaultSellingCurrency != null ? tenant.DefaultSellingCurrency.Symbol : null
            })
            .ToListAsync(ct);

        return new PublicTenantClientInfoResponseDto
        {
            TenantId = tenant.Id,
            TenantName = tenant.Name,
            TenantCode = tenant.Code,
            DefaultCurrencyCode = tenant.DefaultSellingCurrency?.Code ?? string.Empty,
            DefaultCurrencySymbol = tenant.DefaultSellingCurrency?.Symbol ?? string.Empty,
            Clients = clients
        };
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
