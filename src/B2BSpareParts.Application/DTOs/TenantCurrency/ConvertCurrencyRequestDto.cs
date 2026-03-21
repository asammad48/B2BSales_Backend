namespace B2BSpareParts.Application.DTOs.TenantCurrency;

public class ConvertCurrencyRequestDto
{
    public Guid FromCurrencyId { get; set; }
    public Guid ToCurrencyId { get; set; }
    public decimal Amount { get; set; }
}
