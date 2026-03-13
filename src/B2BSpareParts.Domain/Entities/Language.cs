using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class Language : BaseEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsRtl { get; set; }
    public bool IsActive { get; set; } = true;
}
