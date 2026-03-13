using B2BSpareParts.Domain.Common;
using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Domain.Entities;

public class Order : TenantEntity
{
    public Guid ShopId { get; set; }
    public Guid ClientId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid? PlacedByUserId { get; set; }
    public Guid? PreparedByUserId { get; set; }
    public string? Notes { get; set; }

    public Shop? Shop { get; set; }
    public Client? Client { get; set; }
    public Currency? Currency { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
