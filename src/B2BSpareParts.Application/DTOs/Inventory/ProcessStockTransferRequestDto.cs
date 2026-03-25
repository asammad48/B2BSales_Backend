namespace B2BSpareParts.Application.DTOs.Inventory;

public class ProcessStockTransferRequestDto
{
    public List<CreateStockTransferItemRequestDto> Items { get; set; } = [];
}
