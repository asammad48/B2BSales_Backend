namespace B2BSpareParts.Application.DTOs.Pos;

public class CreatePosOrderResponseItemDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public string Sku { get; set; } = default!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
