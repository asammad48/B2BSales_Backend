using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Lookups;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class LookupService : ILookupService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public LookupService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<LookupBundleResponseDto> GetBundleAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        return new LookupBundleResponseDto
        {
            Categories = await _db.Categories.Where(x => x.TenantId == tenantId && !x.IsDeleted).OrderBy(x => x.Name).Select(x => new LookupItemResponseDto { Id = x.Id, Name = x.Name }).ToListAsync(ct),
            Brands = await _db.Brands.Where(x => x.TenantId == tenantId && !x.IsDeleted).OrderBy(x => x.Name).Select(x => new LookupItemResponseDto { Id = x.Id, Name = x.Name }).ToListAsync(ct),
            Models = await _db.DeviceModels.Where(x => x.TenantId == tenantId && !x.IsDeleted).OrderBy(x => x.Name).Select(x => new LookupItemResponseDto { Id = x.Id, Name = x.Name }).ToListAsync(ct),
            PartTypes = await _db.PartTypes.Where(x => x.TenantId == tenantId && !x.IsDeleted).OrderBy(x => x.Name).Select(x => new LookupItemResponseDto { Id = x.Id, Name = x.Name }).ToListAsync(ct),
            Products = await _db.Products.Where(x => x.TenantId == tenantId && !x.IsDeleted).OrderBy(x => x.Name).Select(x => new ProductLookupResponseDto
            {
                Id = x.Id,
                Name = x.Name,
                Sku = x.Sku,
                BrandName = x.Brand != null ? x.Brand.Name : null,
                ModelName = x.Model != null ? x.Model.Name : null,
                Barcode = x.Barcode,
                IsActive = x.IsActive
            }).ToListAsync(ct),
            Shops = await _db.Shops.Where(x => x.TenantId == tenantId && !x.IsDeleted).OrderByDescending(x => x.IsMain).ThenBy(x => x.Name).Select(x => new ShopLookupResponseDto { Id = x.Id, Name = x.Name, Code = x.Code, IsMain = x.IsMain, IsActive = x.IsActive }).ToListAsync(ct),
            Clients = await _db.Clients.Where(x => x.TenantId == tenantId && !x.IsDeleted).OrderBy(x => x.BusinessName).Select(x => new LookupItemResponseDto { Id = x.Id, Name = x.BusinessName }).ToListAsync(ct),
            Currencies = await _db.Currencies.Where(x => !x.IsDeleted).OrderBy(x => x.Name).Select(x => new CurrencyLookupResponseDto { Id = x.Id, Name = x.Name, Code = x.Code, Symbol = x.Symbol }).ToListAsync(ct),
            Languages = await _db.Languages.Where(x => !x.IsDeleted).OrderBy(x => x.Name).Select(x => new LanguageLookupResponseDto { Id = x.Id, Name = x.Name, Code = x.Code, IsRtl = x.IsRtl }).ToListAsync(ct)
        };
    }
}
