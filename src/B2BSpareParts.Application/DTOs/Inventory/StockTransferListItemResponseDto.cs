using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Inventory;

public class StockTransferListItemResponseDto
{
    public Guid Id { get; set; }
    public Guid SourceShopId { get; set; }
    public string SourceShopName { get; set; } = default!;
    public Guid DestinationShopId { get; set; }
    public string DestinationShopName { get; set; } = default!;
    public StockTransferStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public List<StockTransferItemResponseDto> Items { get; set; } = [];
}

public class StockTransferItemResponseDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public int Quantity { get; set; }
    public List<string> Barcodes { get; set; } = [];
}
