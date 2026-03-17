using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Products;

public class ProductPricingAdjustmentResultDto
{
    public Guid ProductId { get; set; }
    public decimal BuyingPrice { get; set; }
    public decimal SellingPrice { get; set; }
    public PricingMode PricingMode { get; set; }
    public decimal? MarkupPercentage { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
