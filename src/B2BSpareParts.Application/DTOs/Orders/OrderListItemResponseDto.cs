namespace B2BSpareParts.Application.DTOs.Orders;

public class OrderListItemResponseDto
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = default!;
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = default!;
    public Guid ClientId { get; set; }
    public string ClientName { get; set; } = default!;
    public string Status { get; set; } = default!;
    public Guid CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = default!;
    public decimal TotalAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
