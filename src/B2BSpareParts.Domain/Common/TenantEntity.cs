namespace B2BSpareParts.Domain.Common;

public abstract class TenantEntity : BaseEntity
{
    public Guid TenantId { get; set; }
}
