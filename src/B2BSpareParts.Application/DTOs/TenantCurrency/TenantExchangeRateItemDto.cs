namespace B2BSpareParts.Application.DTOs.TenantCurrency;

public class TenantExchangeRateItemDto
{
    public Guid Id { get; set; }
    public Guid FromCurrencyId { get; set; }
    public string FromCurrencyCode { get; set; } = default!;
    public Guid ToCurrencyId { get; set; }
    public string ToCurrencyCode { get; set; } = default!;
    public decimal Rate { get; set; }
}
