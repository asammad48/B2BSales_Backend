namespace B2BSpareParts.Application.DTOs.TenantCurrency;

public class ConvertCurrencyResponseDto
{
    public decimal ConvertedAmount { get; set; }
    public decimal Rate { get; set; }
    public string FromCurrencyCode { get; set; } = default!;
    public string ToCurrencyCode { get; set; } = default!;
}
