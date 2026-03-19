using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class DeviceModel : TenantEntity
{
    public Guid BrandId { get; set; }
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public Brand? Brand { get; set; }
}
