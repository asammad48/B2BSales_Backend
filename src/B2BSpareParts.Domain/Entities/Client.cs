using B2BSpareParts.Domain.Common;
using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Domain.Entities;

public class Client : TenantEntity
{
    public string Name { get; set; } = default!;
    public string BusinessName { get; set; } = default!;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public ClientStatus Status { get; set; } = ClientStatus.PendingApproval;
    public Guid? PreferredCurrencyId { get; set; }
    public Guid? PreferredLanguageId { get; set; }

    public Currency? PreferredCurrency { get; set; }
    public Language? PreferredLanguage { get; set; }
}
