using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class Shop : TenantEntity
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public bool IsMain { get; set; }
    public bool IsActive { get; set; } = true;
}
