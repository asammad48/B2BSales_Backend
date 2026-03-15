namespace B2BSpareParts.Application.DTOs.Orders.ClientOrders;

public class PlaceClientOrderRequestDto
{
    public Guid ShopId { get; set; }
    public string? Notes { get; set; }
    public List<PlaceClientOrderItemRequestDto> Items { get; set; } = [];
}
