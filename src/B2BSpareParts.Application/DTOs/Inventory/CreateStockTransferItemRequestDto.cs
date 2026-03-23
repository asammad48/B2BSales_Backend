using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Inventory;

public class CreateStockTransferItemRequestDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public List<string> Barcodes { get; set; } = [];
}
