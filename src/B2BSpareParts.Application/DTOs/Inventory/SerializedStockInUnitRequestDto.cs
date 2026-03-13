using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Application.DTOs.Inventory;

public class SerializedStockInUnitRequestDto
{
    public string UnitBarcode { get; set; } = default!;
    public string? SerialNumber { get; set; }
    public string? Imei1 { get; set; }
    public string? Imei2 { get; set; }
    public decimal? SalePrice { get; set; }
}
