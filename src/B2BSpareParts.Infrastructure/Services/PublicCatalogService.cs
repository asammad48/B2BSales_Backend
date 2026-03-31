using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.PublicCatalog;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Domain.Enums;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class PublicCatalogService : IPublicCatalogService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public PublicCatalogService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PublicCatalogFiltersResponseDto> GetFiltersAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var baseQuery = _db.Products
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted && x.IsPublicVisible);

        var categories = await baseQuery
            .Where(x => x.CategoryId != null && x.Category != null)
            .GroupBy(x => new { Id = x.CategoryId!.Value, x.Category!.Name, x.Category.Code })
            .Select(g => new PublicLookupItemDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                Code = g.Key.Code,
                ProductCount = g.Count()
            })
            .ToListAsync(ct);

        var brands = await baseQuery
            .Where(x => x.BrandId != null)
            .GroupBy(x => new { Id = x.BrandId!.Value, x.Brand!.Name, x.Brand.Code })
            .Select(g => new PublicLookupItemDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                Code = g.Key.Code,
                ProductCount = g.Count()
            })
            .ToListAsync(ct);

        var models = await baseQuery
            .Where(x => x.ModelId != null)
            .GroupBy(x => new { Id = x.ModelId!.Value, x.Model!.Name, x.Model.Code })
            .Select(g => new PublicLookupItemDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                Code = g.Key.Code,
                ProductCount = g.Count()
            })
            .ToListAsync(ct);

        var partTypes = await baseQuery
            .Where(x => x.PartTypeId != null)
            .GroupBy(x => new { Id = x.PartTypeId!.Value, x.PartType!.Name, x.PartType.Code })
            .Select(g => new PublicLookupItemDto
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                Code = g.Key.Code,
                ProductCount = g.Count()
            })
            .ToListAsync(ct);

        return new PublicCatalogFiltersResponseDto
        {
            Categories = categories,
            Brands = brands,
            Models = models,
            PartTypes = partTypes
        };
    }

    public async Task<PublicCatalogLookupsResponseDto> GetLookupsAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new PublicLookupItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code
            })
            .ToListAsync(ct);

        var brands = await _db.Brands
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new PublicLookupItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code
            })
            .ToListAsync(ct);

        var models = await _db.DeviceModels
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new PublicLookupItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code
            })
            .ToListAsync(ct);

        var partTypes = await _db.PartTypes
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.Name)
            .Select(x => new PublicLookupItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code
            })
            .ToListAsync(ct);

        return new PublicCatalogLookupsResponseDto
        {
            Categories = categories,
            Brands = brands,
            Models = models,
            PartTypes = partTypes
        };
    }

    public async Task<PageResponse<PublicProductListItemDto>> GetProductsAsync(GetPublicProductsRequestDto request, CancellationToken ct = default)
    {
        var isGuestView = _tenantContext.UserId == null;
        var tenantId = _tenantContext.TenantId;
        var query = _db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Model)
            .Include(x => x.PartType)
            .Include(x => x.Images)
            .Include(x => x.Tenant)
            .ThenInclude(t => t!.BaseCurrency)
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted && x.IsPublicVisible);

        if (request.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);

        if (request.BrandId.HasValue)
            query = query.Where(x => x.BrandId == request.BrandId.Value);

        if (request.ModelId.HasValue)
            query = query.Where(x => x.ModelId == request.ModelId.Value);

        if (request.PartTypeId.HasValue)
            query = query.Where(x => x.PartTypeId == request.PartTypeId.Value);


        if (request.ShopId.HasValue)
        {
            query = query.Where(x =>
                (x.TrackingType == TrackingType.Serializado &&
                    _db.SerializedInventoryUnits.Any(u =>
                        u.TenantId == tenantId &&
                        u.ShopId == request.ShopId.Value &&
                        u.ProductId == x.Id &&
                        u.Status == SerializedUnitStatus.InStock &&
                        !u.IsDeleted)) ||
                (x.TrackingType != TrackingType.Serializado &&
                    _db.ShopInventories.Any(i =>
                        i.TenantId == tenantId &&
                        i.ShopId == request.ShopId.Value &&
                        i.ProductId == x.Id &&
                        i.QuantityOnHand > 0 &&
                        !i.IsDeleted))
            );
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.Sku.ToLower().Contains(search) ||
                (x.Barcode != null && x.Barcode.ToLower().Contains(search)) ||
                (x.Brand != null && x.Brand.Name.ToLower().Contains(search)) ||
                (x.Model != null && x.Model.Name.ToLower().Contains(search)) ||
                (x.PartType != null && x.PartType.Name.ToLower().Contains(search))
            );
        }

        var projected = query
            .ApplyCreatedAtSort(request)
            .Select<Product, PublicProductListItemDto>(x => new PublicProductListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortDescription = x.ShortDescription,
                Sku = x.Sku,
                Barcode = x.Barcode,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : string.Empty,
                BrandId = x.BrandId,
                BrandName = x.Brand != null ? x.Brand.Name : null,
                ModelId = x.ModelId,
                ModelName = x.Model != null ? x.Model.Name : null,
                PartTypeId = x.PartTypeId,
                PartTypeName = x.PartType != null ? x.PartType.Name : null,
                PrimaryImageUrl = x.Images.Where(i => i.IsPrimary).OrderBy(i => i.SortOrder).Select(i => i.FilePath).FirstOrDefault(),
                Price = x.DefaultSellingPrice,
                CurrencyCode = x.Tenant!.BaseCurrency!.Code,
                StockQuantity = x.TrackingType == TrackingType.Serializado
                    ? _db.SerializedInventoryUnits.Count(u =>
                        u.TenantId == tenantId &&
                        u.ProductId == x.Id &&
                        u.Status == SerializedUnitStatus.InStock &&
                        (!request.ShopId.HasValue || u.ShopId == request.ShopId.Value) &&
                        !u.IsDeleted)
                    : _db.ShopInventories
                        .Where(i =>
                            i.TenantId == tenantId &&
                            i.ProductId == x.Id &&
                            (!request.ShopId.HasValue || i.ShopId == request.ShopId.Value) &&
                            !i.IsDeleted)
                        .Sum(i => i.QuantityOnHand),
                QuantityInHand = x.TrackingType == TrackingType.Serializado
                    ? _db.SerializedInventoryUnits.Count(u =>
                        u.TenantId == tenantId &&
                        u.ProductId == x.Id &&
                        u.Status == SerializedUnitStatus.InStock &&
                        (!request.ShopId.HasValue || u.ShopId == request.ShopId.Value) &&
                        !u.IsDeleted)
                    : _db.ShopInventories
                        .Where(i =>
                            i.TenantId == tenantId &&
                            i.ProductId == x.Id &&
                            (!request.ShopId.HasValue || i.ShopId == request.ShopId.Value) &&
                            !i.IsDeleted)
                        .Sum(i => i.QuantityOnHand),
                IsInStock = x.TrackingType == TrackingType.Serializado
                    ? _db.SerializedInventoryUnits.Any(u =>
                        u.TenantId == tenantId &&
                        u.ProductId == x.Id &&
                        u.Status == SerializedUnitStatus.InStock &&
                        (!request.ShopId.HasValue || u.ShopId == request.ShopId.Value) &&
                        !u.IsDeleted)
                    : _db.ShopInventories.Any(i =>
                        i.TenantId == tenantId &&
                        i.ProductId == x.Id &&
                        i.QuantityOnHand > 0 &&
                        (!request.ShopId.HasValue || i.ShopId == request.ShopId.Value) &&
                        !i.IsDeleted),
                IsPriceLocked = isGuestView,
                CanOrder = !isGuestView,
                Slug = null // Field not yet implemented in Product entity
            });

        return await projected.ToPageAsync(request, ct);
    }

    public async Task<PageResponse<PublicNewArrivalProductItemDto>> GetNewArrivalsAsync(GetPublicProductsRequestDto request, CancellationToken ct = default)
    {
        var query = BuildPublicCatalogQuery(request.ShopId);

        if (request.CategoryId.HasValue)
            query = query.Where(x => x.CategoryId == request.CategoryId.Value);

        if (request.BrandId.HasValue)
            query = query.Where(x => x.BrandId == request.BrandId.Value);

        if (request.ModelId.HasValue)
            query = query.Where(x => x.ModelId == request.ModelId.Value);

        if (request.PartTypeId.HasValue)
            query = query.Where(x => x.PartTypeId == request.PartTypeId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.Sku.ToLower().Contains(search) ||
                (x.Barcode != null && x.Barcode.ToLower().Contains(search)) ||
                (x.Brand != null && x.Brand.Name.ToLower().Contains(search)) ||
                (x.Model != null && x.Model.Name.ToLower().Contains(search)) ||
                (x.PartType != null && x.PartType.Name.ToLower().Contains(search))
            );
        }

        return await BuildPublicNewArrivalPageAsync(query.OrderByDescending(x => x.CreatedAt), request, request.ShopId, ct);
    }

    public async Task<PageResponse<PublicNewArrivalProductItemDto>> GetFeaturedProductsAsync(GetFeaturedProductsRequestDto request, CancellationToken ct = default)
    {
        if (!request.ShopId.HasValue || request.ShopId.Value == Guid.Empty)
            throw new AppException("ShopId is required", 400);

        IQueryable<Product> query = BuildPublicCatalogQuery(request.ShopId)
            .Where(x => x.IsFeatured);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Name.ToLower().Contains(search) ||
                x.Sku.ToLower().Contains(search) ||
                (x.Barcode != null && x.Barcode.ToLower().Contains(search)) ||
                (x.Brand != null && x.Brand.Name.ToLower().Contains(search)) ||
                (x.Model != null && x.Model.Name.ToLower().Contains(search)) ||
                (x.PartType != null && x.PartType.Name.ToLower().Contains(search))
            );
        }

        return await BuildPublicNewArrivalPageAsync(query.OrderByDescending(x => x.CreatedAt), request, request.ShopId, ct);
    }

    private IQueryable<Product> BuildPublicCatalogQuery(Guid? shopId)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Model)
            .Include(x => x.PartType)
            .Include(x => x.Images)
            .Include(x => x.Tenant)
            .ThenInclude(t => t!.BaseCurrency)
            .Where(x => x.TenantId == tenantId && x.IsActive && !x.IsDeleted && x.IsPublicVisible);

        if (shopId.HasValue)
        {
            query = query.Where(x =>
                (x.TrackingType == TrackingType.Serializado &&
                    _db.SerializedInventoryUnits.Any(u =>
                        u.TenantId == tenantId &&
                        u.ShopId == shopId.Value &&
                        u.ProductId == x.Id &&
                        u.Status == SerializedUnitStatus.InStock &&
                        !u.IsDeleted)) ||
                (x.TrackingType != TrackingType.Serializado &&
                    _db.ShopInventories.Any(i =>
                        i.TenantId == tenantId &&
                        i.ShopId == shopId.Value &&
                        i.ProductId == x.Id &&
                        i.QuantityOnHand > 0 &&
                        !i.IsDeleted))
            );
        }

        return query;
    }

    private async Task<PageResponse<PublicNewArrivalProductItemDto>> BuildPublicNewArrivalPageAsync(IQueryable<Product> query, PageRequest request, Guid? shopId, CancellationToken ct)
    {
        var isGuestView = _tenantContext.UserId == null;
        var tenantId = _tenantContext.TenantId;

        var projected = query
            .Select<Product, PublicNewArrivalProductItemDto>(x => new PublicNewArrivalProductItemDto
            {
                Id = x.Id,
                Name = x.Name,
                ShortDescription = x.ShortDescription,
                Sku = x.Sku,
                Barcode = x.Barcode,
                CategoryId = x.CategoryId,
                CategoryName = x.Category != null ? x.Category.Name : string.Empty,
                BrandId = x.BrandId,
                BrandName = x.Brand != null ? x.Brand.Name : null,
                ModelId = x.ModelId,
                ModelName = x.Model != null ? x.Model.Name : null,
                PartTypeId = x.PartTypeId,
                PartTypeName = x.PartType != null ? x.PartType.Name : null,
                PrimaryImageUrl = x.Images.Where(i => i.IsPrimary).OrderBy(i => i.SortOrder).Select(i => i.FilePath).FirstOrDefault(),
                Price = x.DefaultSellingPrice,
                CurrencyCode = x.Tenant!.BaseCurrency!.Code,
                StockQuantity = x.TrackingType == TrackingType.Serializado
                    ? _db.SerializedInventoryUnits.Count(u =>
                        u.TenantId == tenantId &&
                        u.ProductId == x.Id &&
                        u.Status == SerializedUnitStatus.InStock &&
                        (!shopId.HasValue || u.ShopId == shopId.Value) &&
                        !u.IsDeleted)
                    : _db.ShopInventories
                        .Where(i =>
                            i.TenantId == tenantId &&
                            i.ProductId == x.Id &&
                            (!shopId.HasValue || i.ShopId == shopId.Value) &&
                            !i.IsDeleted)
                        .Sum(i => i.QuantityOnHand),
                QuantityInHand = x.TrackingType == TrackingType.Serializado
                    ? _db.SerializedInventoryUnits.Count(u =>
                        u.TenantId == tenantId &&
                        u.ProductId == x.Id &&
                        u.Status == SerializedUnitStatus.InStock &&
                        (!shopId.HasValue || u.ShopId == shopId.Value) &&
                        !u.IsDeleted)
                    : _db.ShopInventories
                        .Where(i =>
                            i.TenantId == tenantId &&
                            i.ProductId == x.Id &&
                            (!shopId.HasValue || i.ShopId == shopId.Value) &&
                            !i.IsDeleted)
                        .Sum(i => i.QuantityOnHand),
                IsInStock = x.TrackingType == TrackingType.Serializado
                    ? _db.SerializedInventoryUnits.Any(u =>
                        u.TenantId == tenantId &&
                        u.ProductId == x.Id &&
                        u.Status == SerializedUnitStatus.InStock &&
                        (!shopId.HasValue || u.ShopId == shopId.Value) &&
                        !u.IsDeleted)
                    : _db.ShopInventories.Any(i =>
                        i.TenantId == tenantId &&
                        i.ProductId == x.Id &&
                        i.QuantityOnHand > 0 &&
                        (!shopId.HasValue || i.ShopId == shopId.Value) &&
                        !i.IsDeleted),
                IsPriceLocked = isGuestView,
                CanOrder = !isGuestView,
                Slug = null,
                CreatedAt = x.CreatedAt
            });

        return await projected.ToPageAsync(request, ct);
    }

}
