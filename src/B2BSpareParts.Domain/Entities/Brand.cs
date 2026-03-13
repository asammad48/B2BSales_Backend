using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class Brand : TenantEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
}
