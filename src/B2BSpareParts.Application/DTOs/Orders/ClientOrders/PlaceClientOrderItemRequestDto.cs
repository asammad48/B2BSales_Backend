namespace B2BSpareParts.Application.DTOs.Orders.ClientOrders;

public class PlaceClientOrderItemRequestDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
