using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Inventory;

public class StockInRequestDto
{
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal BuyingPrice { get; set; }
    public decimal? SellingPrice { get; set; }
    public PricingMode PricingMode { get; set; }
    public decimal? MarkupPercentage { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1;
    public bool UpdateProductDefaultSellingPrice { get; set; }
    public List<SerializedStockInUnitRequestDto> SerializedUnits { get; set; } = [];
}
