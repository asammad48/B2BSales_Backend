using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Products;

public class AdjustProductPricingRequestDto
{
    public Guid? BaseCurrencyId { get; set; }
    public decimal? BasePrice { get; set; }
    public decimal BuyingPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public PricingMode? PricingMode { get; set; }
    public decimal? MarkupPercentage { get; set; }
    public string? Reason { get; set; }
    public bool UpdateDefaultPrice { get; set; }
}
