using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities;

public class StockTransferItem : BaseEntity
{
    public Guid StockTransferId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public string? SelectedUnitBarcodesJson { get; set; }

    public StockTransfer? StockTransfer { get; set; }
    public Product? Product { get; set; }
}
