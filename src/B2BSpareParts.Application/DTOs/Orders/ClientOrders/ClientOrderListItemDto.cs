using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Orders.ClientOrders;

public class ClientOrderListItemDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public Guid ClientId { get; set; }
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = default!;
    public OrderStatus Status { get; set; }
    public string StatusLabel => Status.ToString();
    public string CurrencyCode { get; set; } = default!;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? PickupConfirmedAt { get; set; }
    public string? Notes { get; set; }
}
