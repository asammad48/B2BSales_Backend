using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.Products;
using B2BSpareParts.Domain.Entities;
using B2BSpareParts.Domain.Enums;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace B2BSpareParts.Infrastructure.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;

    public ProductService(AppDbContext db, ITenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<PageResponse<ProductListItemResponseDto>> GetPagedAsync(PageRequest request, CancellationToken ct = default)
    {
        var isGuestView = _tenantContext.UserId == null;
        var tenantId = _tenantContext.TenantId;
        var query = _db.Products
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted);

        if (isGuestView)
        {
            query = query.Where(x => x.IsPublicVisible && x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(search) || x.Sku.ToLower().Contains(search) || (x.Barcode != null && x.Barcode.ToLower().Contains(search)));
        }

        var projected = query
            .ApplyCreatedAtSort(request)
            .Select(x => new ProductListItemResponseDto
            {
                Id = x.Id,
                Sku = x.Sku,
                Barcode = x.Barcode,
                Name = x.Name,
                CategoryId = x.CategoryId,
                CategoryName = x.Category!.Name,
                BrandId = x.BrandId,
                BrandName = x.Brand != null ? x.Brand.Name : null,
                ModelId = x.ModelId,
                ModelName = x.Model != null ? x.Model.Name : null,
                PartTypeId = x.PartTypeId,
                PartTypeName = x.PartType != null ? x.PartType.Name : null,
                TrackingType = x.TrackingType,
                QualityType = x.QualityType,
                DefaultSellingPrice = x.DefaultSellingPrice,
                PrimaryImageUrl = x.Images.Where(i => i.IsPrimary).OrderBy(i => i.SortOrder).Select(i => i.FilePath).FirstOrDefault(),
                IsActive = x.IsActive,
                SortOrder = x.SortOrder,
                IsPriceLocked = isGuestView,
                CanOrder = !isGuestView
            });

        return await projected.ToPageAsync(request, ct);
    }

    public async Task<ProductDetailResponseDto> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var isGuestView = _tenantContext.UserId == null;
        var tenantId = _tenantContext.TenantId;
        var product = await _db.Products
            .AsNoTracking()
            .Include(x => x.Category)
            .Include(x => x.Brand)
            .Include(x => x.Model)
            .Include(x => x.PartType)
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new AppException("Product not found", 404);

        var related = await _db.ProductRelations
            .AsNoTracking()
            .Include(x => x.RelatedProduct)
            .Where(x => x.ProductId == product.Id && x.TenantId == tenantId && !x.IsDeleted)
            .OrderBy(x => x.SortOrder)
            .Select(x => new RelatedProductResponseDto
            {
                ProductId = x.RelatedProductId,
                Name = x.RelatedProduct!.Name,
                RelationType = x.RelationType.ToString()
            })
            .ToListAsync(ct);

        return new ProductDetailResponseDto
        {
            Id = product.Id,
            Sku = product.Sku,
            Barcode = product.Barcode,
            Name = product.Name,
            CategoryId = product.CategoryId,
            CategoryName = product.Category!.Name,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name,
            ModelId = product.ModelId,
            ModelName = product.Model?.Name,
            PartTypeId = product.PartTypeId,
            PartTypeName = product.PartType?.Name,
            TrackingType = product.TrackingType,
            QualityType = product.QualityType,
            DefaultBuyingPrice = isGuestView ? null : product.DefaultBuyingPrice,
            DefaultSellingPrice = isGuestView ? null : product.DefaultSellingPrice,
            DefaultPricingMode = product.DefaultPricingMode,
            DefaultMarkupPercentage = isGuestView ? null : product.DefaultMarkupPercentage,
            WarrantyDays = product.WarrantyDays,
            LowStockThreshold = product.LowStockThreshold,
            ShortDescription = product.ShortDescription,
            LongDescription = product.LongDescription,
            Specifications = product.Specifications,
            Images = product.Images.OrderBy(x => x.SortOrder).Select(x => new ProductImageResponseDto
            {
                Id = x.Id,
                FilePath = x.FilePath,
                AltText = x.AltText,
                IsPrimary = x.IsPrimary
            }).ToList(),
            RelatedProducts = related,
            IsPriceLocked = isGuestView,
            CanOrder = !isGuestView,
            SortOrder = product.SortOrder,
            AvailabilityMessage = isGuestView ? "Login required to view price and place order." : "Available for approved logged-in clients."
        };
    }

    public async Task<PageResponse<ProductLookupItemDto>> GetLookupAsync(PageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var query = _db.Products
            .AsNoTracking()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted && x.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(s) || x.Sku.ToLower().Contains(s));
        }

        var projected = query
            .OrderBy(x => x.Name)
            .Select(x => new ProductLookupItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Sku = x.Sku,
                BrandName = x.Brand != null ? x.Brand.Name : null,
                ModelName = x.Model != null ? x.Model.Name : null,
                TrackingType = x.TrackingType
            });

        return await projected.ToPageAsync(request, ct);
    }

    public async Task<Guid> CreateAsync(CreateProductRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty) throw new AppException("Tenant context missing", 400);

        if (request.Images == null || !request.Images.Any())
        {
            throw new AppException("At least one image is required.", 400);
        }

        if (request.Images.Count(x => x.IsPrimary) != 1)
        {
            throw new AppException("Exactly one image must be marked as primary.", 400);
        }

        var product = new Product
        {
            TenantId = tenantId,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            ModelId = request.ModelId,
            PartTypeId = request.PartTypeId,
            Sku = request.Sku.Trim(),
            Barcode = request.Barcode?.Trim(),
            Name = request.Name.Trim(),
            ShortDescription = request.ShortDescription,
            LongDescription = request.LongDescription,
            Specifications = request.Specifications,
            TrackingType = request.TrackingType,
            QualityType = request.QualityType,
            DefaultBuyingPrice = request.DefaultBuyingPrice,
            DefaultPricingMode = request.DefaultPricingMode,
            DefaultMarkupPercentage = request.DefaultMarkupPercentage ?? 0,
            WarrantyDays = request.WarrantyDays,
            LowStockThreshold = request.LowStockThreshold,
            SortOrder = request.SortOrder,
            Images = request.Images.Select(x => new ProductImage
            {
                TenantId = tenantId,
                FilePath = x.FilePath,
                AltText = x.AltText,
                IsPrimary = x.IsPrimary,
                SortOrder = x.SortOrder
            }).ToList()
        };

        if (product.DefaultPricingMode == PricingMode.PercentageBased)
        {
            product.DefaultSellingPrice = product.DefaultBuyingPrice + (product.DefaultBuyingPrice * (product.DefaultMarkupPercentage ?? 0) / 100m);
        }
        else
        {
            product.DefaultSellingPrice = request.DefaultSellingPrice;
        }

        _db.Products.Add(product);
        await _db.SaveChangesAsync(ct);
        return product.Id;
    }

    public async Task<ProductPricingAdjustmentResultDto> AdjustPricingAsync(Guid productId, AdjustProductPricingRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var product = await _db.Products
            .FirstOrDefaultAsync(x => x.Id == productId && x.TenantId == tenantId && !x.IsDeleted, ct)
            ?? throw new AppException("Product not found", 404);

        product.DefaultBuyingPrice = request.BuyingPrice;

        if (request.PricingMode.HasValue)
        {
            product.DefaultPricingMode = request.PricingMode.Value;
        }

        product.DefaultMarkupPercentage = request.MarkupPercentage ?? 0;

        if (product.DefaultPricingMode == PricingMode.PercentageBased)
        {
            product.DefaultSellingPrice = product.DefaultBuyingPrice + (product.DefaultBuyingPrice * (product.DefaultMarkupPercentage ?? 0) / 100m);
        }
        else
        {
            product.DefaultSellingPrice = request.SellingPrice;
        }

        product.UpdatedAt = DateTimeOffset.UtcNow;
        // Logic for auditing "Reason" could be added here if an audit table existed.

        await _db.SaveChangesAsync(ct);

        return new ProductPricingAdjustmentResultDto
        {
            ProductId = product.Id,
            BuyingPrice = product.DefaultBuyingPrice,
            SellingPrice = product.DefaultSellingPrice,
            PricingMode = product.DefaultPricingMode,
            MarkupPercentage = product.DefaultMarkupPercentage,
            UpdatedAt = product.UpdatedAt ?? DateTimeOffset.UtcNow
        };
    }
}
