using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal BaseUnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? SelectedUnitBarcodesJson { get; set; }

    public Order? Order { get; set; }
    public Product? Product { get; set; }
}
