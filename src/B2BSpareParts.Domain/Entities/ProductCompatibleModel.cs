using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class ProductCompatibleModel : TenantEntity
{
    public Guid ProductId { get; set; }
    public Guid ModelId { get; set; }

    public Product? Product { get; set; }
    public DeviceModel? Model { get; set; }
}
