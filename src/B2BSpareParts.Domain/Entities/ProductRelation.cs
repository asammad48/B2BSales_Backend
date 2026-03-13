using B2BSpareParts.Domain.Common;
using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Domain.Entities;

public class ProductRelation : TenantEntity
{
    public Guid ProductId { get; set; }
    public Guid RelatedProductId { get; set; }
    public ProductRelationType RelationType { get; set; }
    public int SortOrder { get; set; }

    public Product? Product { get; set; }
    public Product? RelatedProduct { get; set; }
}
