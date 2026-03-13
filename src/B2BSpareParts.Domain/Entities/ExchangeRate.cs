using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class ExchangeRate : TenantEntity
{
    public Guid FromCurrencyId { get; set; }
    public Guid ToCurrencyId { get; set; }
    public decimal Rate { get; set; }
    public DateTime EffectiveDate { get; set; }

    public Currency? FromCurrency { get; set; }
    public Currency? ToCurrency { get; set; }
}
