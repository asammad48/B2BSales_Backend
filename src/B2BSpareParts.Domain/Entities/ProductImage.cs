using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class ProductImage : TenantEntity
{
    public Guid ProductId { get; set; }
    public string FilePath { get; set; } = default!;
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }

    public Product? Product { get; set; }
}
