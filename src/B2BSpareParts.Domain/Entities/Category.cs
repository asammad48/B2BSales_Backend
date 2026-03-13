using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class Category : TenantEntity
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public Guid? ParentCategoryId { get; set; }
    public bool IsActive { get; set; } = true;

    public Category? ParentCategory { get; set; }
}
