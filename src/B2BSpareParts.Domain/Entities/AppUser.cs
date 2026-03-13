using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class AppUser : TenantEntity
{
    public Guid? ShopId { get; set; }
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
    public string PasswordHash { get; set; } = default!;
    public string Role { get; set; } = default!;
    public Guid? PreferredLanguageId { get; set; }
    public bool IsActive { get; set; } = true;

    public Shop? Shop { get; set; }
}
