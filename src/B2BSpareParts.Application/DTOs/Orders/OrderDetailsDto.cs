namespace B2BSpareParts.Application.DTOs.Orders;

public class OrderDetailsDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = default!;
    public string BusinessName { get; set; } = default!;
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string StatusLabel { get; set; } = default!;
    public string CurrencyCode { get; set; } = default!;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadyAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public List<OrderDetailsItemDto> Items { get; set; } = new();
}
