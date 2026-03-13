using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Inventory;

public class CreateStockTransferRequestDto
{
    public Guid SourceShopId { get; set; }
    public Guid DestinationShopId { get; set; }
    public string? Notes { get; set; }
    public List<CreateStockTransferItemRequestDto> Items { get; set; } = [];
}
