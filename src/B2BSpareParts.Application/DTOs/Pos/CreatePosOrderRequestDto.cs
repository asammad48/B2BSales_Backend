namespace B2BSpareParts.Application.DTOs.Pos;

public class CreatePosOrderRequestDto
{
    public Guid ShopId { get; set; }
    public Guid? ClientId { get; set; }
    public string? Notes { get; set; }
    public List<CreatePosOrderItemDto> Items { get; set; } = [];
}
