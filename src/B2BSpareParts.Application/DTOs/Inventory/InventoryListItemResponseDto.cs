using B2BSpareParts.Application.DTOs.Common;

namespace B2BSpareParts.Application.DTOs.Inventory;

public class InventoryListItemResponseDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = default!;
    public Guid ShopId { get; set; }
    public string ShopName { get; set; } = default!;
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public Guid? ModelId { get; set; }
    public string? ModelName { get; set; }
    public string Sku { get; set; } = default!;
    public string? Barcode { get; set; }
    public List<ProductBarcodeDto> Barcodes { get; set; } = [];
    public string TrackingType { get; set; } = default!;
    public int QuantityOnHand { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int LowStockThreshold { get; set; }
}
