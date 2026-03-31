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
                CategoryName = x.Category != null ? x.Category.Name : string.Empty,
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
                QuantityInHand = x.TrackingType == TrackingType.Serializado
                    ? _db.SerializedInventoryUnits.Count(u => u.TenantId == tenantId && u.ProductId == x.Id && u.Status == SerializedUnitStatus.InStock && !u.IsDeleted)
                    : _db.ShopInventories.Where(i => i.TenantId == tenantId && i.ProductId == x.Id && !i.IsDeleted).Sum(i => i.QuantityOnHand),
                IsActive = x.IsActive,
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
            .Include(x => x.BaseCurrency)
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
            CategoryName = product.Category?.Name ?? string.Empty,
            BrandId = product.BrandId,
            BrandName = product.Brand?.Name,
            ModelId = product.ModelId,
            ModelName = product.Model?.Name,
            PartTypeId = product.PartTypeId,
            PartTypeName = product.PartType?.Name,
            TrackingType = product.TrackingType,
            QualityType = product.QualityType,
            BaseCurrencyId = product.BaseCurrencyId,
            BaseCurrencyCode = product.BaseCurrency?.Code ?? string.Empty,
            BasePrice = product.BasePrice,
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
            AvailabilityMessage = isGuestView ? "Login required to view price and place order." : "Available for approved logged-in clients."
        };
    }

    public async Task<Guid> CreateAsync(CreateProductRequestDto request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty) throw new AppException("Tenant context missing", 400);

        if (request.Images == null || request.Images.Count == 0)
        {
            throw new AppException("At least one image is required", 400);
        }

        if (request.Images.Count(x => x.IsPrimary) != 1)
        {
            // If none are marked primary, or more than one, we fix it
            if (request.Images.Count(x => x.IsPrimary) == 0)
            {
                request.Images.First().IsPrimary = true;
            }
            else
            {
                var firstPrimary = request.Images.First(x => x.IsPrimary);
                foreach (var img in request.Images) img.IsPrimary = false;
                firstPrimary.IsPrimary = true;
            }
        }

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new AppException("Tenant not found", 400);

        var rate = 1.0m;
        if (request.BaseCurrencyId != tenant.DefaultSellingCurrencyId)
        {
            var exchangeRate = await _db.ExchangeRates
                .FirstOrDefaultAsync(er => er.TenantId == tenantId &&
                                          er.FromCurrencyId == request.BaseCurrencyId &&
                                          er.ToCurrencyId == tenant.DefaultSellingCurrencyId, ct);

            if (exchangeRate == null)
                throw new AppException("Exchange rate not found for the selected base currency.", 400);

            rate = exchangeRate.Rate;
        }

        var buyingPrice = request.BasePrice * rate;

        var markupPercentage = request.DefaultPricingMode == PricingMode.PercentageBased
            ? (request.DefaultMarkupPercentage ?? 0)
            : (decimal?)null;

        var sellingPrice = request.DefaultPricingMode == PricingMode.PercentageBased
            ? buyingPrice + (buyingPrice * markupPercentage!.Value / 100m)
            : request.DefaultSellingPrice;

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
            BaseCurrencyId = request.BaseCurrencyId,
            BasePrice = request.BasePrice,
            DefaultBuyingPrice = buyingPrice,
            DefaultSellingPrice = sellingPrice,
            DefaultPricingMode = request.DefaultPricingMode,
            DefaultMarkupPercentage = markupPercentage,
            WarrantyDays = request.WarrantyDays,
            LowStockThreshold = request.LowStockThreshold,
            Images = request.Images.OrderBy(x => x.SortOrder).Select(x => new ProductImage
            {
                TenantId = tenantId,
                FilePath = x.FilePath,
                AltText = x.AltText,
                IsPrimary = x.IsPrimary,
                SortOrder = x.SortOrder
            }).ToList()
        };

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

        if (request.BaseCurrencyId.HasValue)
        {
            product.BaseCurrencyId = request.BaseCurrencyId.Value;
        }

        if (request.BasePrice.HasValue)
        {
            product.BasePrice = request.BasePrice.Value;
        }

        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct)
            ?? throw new AppException("Tenant not found", 400);

        var rate = 1.0m;
        if (product.BaseCurrencyId != tenant.DefaultSellingCurrencyId)
        {
            var exchangeRate = await _db.ExchangeRates
                .FirstOrDefaultAsync(er => er.TenantId == tenantId &&
                                          er.FromCurrencyId == product.BaseCurrencyId &&
                                          er.ToCurrencyId == tenant.DefaultSellingCurrencyId, ct);

            if (exchangeRate == null)
                throw new AppException("Exchange rate not found for the selected base currency.", 400);

            rate = exchangeRate.Rate;
        }

        product.DefaultBuyingPrice = product.BasePrice * rate;

        if (request.PricingMode.HasValue)
        {
            product.DefaultPricingMode = request.PricingMode.Value;
        }

        if (product.DefaultPricingMode == PricingMode.PercentageBased)
        {
            product.DefaultMarkupPercentage = request.MarkupPercentage ?? 0;
            product.DefaultSellingPrice = product.DefaultBuyingPrice + (product.DefaultBuyingPrice * product.DefaultMarkupPercentage.Value / 100m);
        }
        else
        {
            product.DefaultMarkupPercentage = null;
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
