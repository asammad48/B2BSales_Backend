namespace B2BSpareParts.Application.DTOs.Pos;

public class CreatePosOrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public List<string> Barcodes { get; set; } = [];
}
