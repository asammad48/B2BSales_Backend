using B2BSpareParts.Domain.Common;
using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Domain.Entities;

public class StockMovement : TenantEntity
{
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? SerializedInventoryUnitId { get; set; }
    public StockMovementType MovementType { get; set; }
    public int Quantity { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Note { get; set; }
    public Guid? PerformedByUserId { get; set; }

    public Shop? Shop { get; set; }
    public Product? Product { get; set; }
    public SerializedInventoryUnit? SerializedInventoryUnit { get; set; }
    public AppUser? PerformedByUser { get; set; }
}
