using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public Guid BaseCurrencyId { get; set; }
    public Guid DefaultSellingCurrencyId { get; set; }
    public Guid DefaultLanguageId { get; set; }
    public bool IsActive { get; set; } = true;

    public Currency? BaseCurrency { get; set; }
    public Currency? DefaultSellingCurrency { get; set; }
    public Language? DefaultLanguage { get; set; }
    public ICollection<Shop> Shops { get; set; } = new List<Shop>();
}
