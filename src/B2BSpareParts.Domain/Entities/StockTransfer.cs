using B2BSpareParts.Domain.Common;
using B2BSpareParts.Domain.Enums;

namespace B2BSpareParts.Domain.Entities;

public class StockTransfer : TenantEntity
{
    public Guid SourceShopId { get; set; }
    public Guid DestinationShopId { get; set; }
    public StockTransferStatus Status { get; set; } = StockTransferStatus.Draft;
    public string? Notes { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? DispatchedByUserId { get; set; }
    public Guid? ReceivedByUserId { get; set; }

    public ICollection<StockTransferItem> Items { get; set; } = new List<StockTransferItem>();
}
