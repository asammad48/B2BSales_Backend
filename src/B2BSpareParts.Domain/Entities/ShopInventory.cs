using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class ShopInventory : TenantEntity
{
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityOnHand { get; set; }
    public int ReservedQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;

    public Shop? Shop { get; set; }
    public Product? Product { get; set; }
}
