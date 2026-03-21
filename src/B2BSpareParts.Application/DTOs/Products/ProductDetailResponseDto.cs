using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Products;

public class ProductDetailResponseDto : ProductListItemResponseDto
{
    public string? ShortDescription { get; set; }
    public string? LongDescription { get; set; }
    public string? Specifications { get; set; }
    public Guid BaseCurrencyId { get; set; }
    public string BaseCurrencyCode { get; set; } = default!;
    public decimal BasePrice { get; set; }
    public decimal? DefaultBuyingPrice { get; set; }
    public PricingMode DefaultPricingMode { get; set; }
    public decimal? DefaultMarkupPercentage { get; set; }
    public int WarrantyDays { get; set; }
    public int LowStockThreshold { get; set; }
    public List<ProductImageResponseDto> Images { get; set; } = [];
    public List<RelatedProductResponseDto> RelatedProducts { get; set; } = [];
    public string? AvailabilityMessage { get; set; }
}
