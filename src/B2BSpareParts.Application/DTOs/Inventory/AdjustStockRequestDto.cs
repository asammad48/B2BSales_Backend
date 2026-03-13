using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Inventory;

public class AdjustStockRequestDto
{
    public Guid ShopId { get; set; }
    public Guid ProductId { get; set; }
    public int QuantityChange { get; set; }
    public string Reason { get; set; } = default!;
}
