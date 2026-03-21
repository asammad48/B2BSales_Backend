namespace B2BSpareParts.Application.DTOs.TenantCurrency;

public class UpsertTenantExchangeRateRequestDto
{
    public Guid FromCurrencyId { get; set; }
    public Guid ToCurrencyId { get; set; }
    public decimal Rate { get; set; }
}
