namespace B2BSpareParts.Application.DTOs.Orders.ClientOrders;

public class PlaceClientOrderResponseDto
{
    public Guid OrderId { get; set; }
    public string OrderNumber { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string Message { get; set; } = default!;
}
