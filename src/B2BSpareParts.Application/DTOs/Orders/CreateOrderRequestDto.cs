namespace B2BSpareParts.Application.DTOs.Orders;

public class CreateOrderRequestDto
{
    public Guid ShopId { get; set; }
    public Guid ClientId { get; set; }
    public Guid CurrencyId { get; set; }
    public decimal ExchangeRate { get; set; } = 1;
    public string? Notes { get; set; }
    public List<CreateOrderItemRequestDto> Items { get; set; } = [];
}
