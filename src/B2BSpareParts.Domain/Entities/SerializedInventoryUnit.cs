using B2BSpareParts.Domain.Common;
using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Domain.Entities;

public class SerializedInventoryUnit : TenantEntity
{
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public string UnitBarcode { get; set; } = default!;
    public string? SerialNumber { get; set; }
    public string? Imei1 { get; set; }
    public string? Imei2 { get; set; }
    public SerializedUnitStatus Status { get; set; } = SerializedUnitStatus.InStock;
    public decimal PurchaseCost { get; set; }
    public decimal? SalePrice { get; set; }
    public string? ConditionNote { get; set; }

    public Shop? Shop { get; set; }
    public Product? Product { get; set; }
}
