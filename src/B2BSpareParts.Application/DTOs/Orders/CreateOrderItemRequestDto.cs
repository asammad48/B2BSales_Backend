namespace B2BSpareParts.Application.DTOs.Orders;

public class CreateOrderItemRequestDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}
