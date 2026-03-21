using B2BSpareParts.Application.DTOs.Lookups;

namespace B2BSpareParts.Application.DTOs.TenantCurrency;

public class TenantCurrencySettingsDto
{
    public Guid DefaultSellingCurrencyId { get; set; }
    public string DefaultSellingCurrencyCode { get; set; } = default!;
    public List<CurrencyLookupResponseDto> AvailableCurrencies { get; set; } = new();
    public List<TenantExchangeRateItemDto> ExchangeRates { get; set; } = new();
}
