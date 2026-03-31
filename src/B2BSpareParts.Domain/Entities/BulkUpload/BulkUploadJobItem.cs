using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities.BulkUpload;

public enum BulkUploadRowStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3
}

public class BulkUploadJobItem : TenantEntity
{
    public Guid JobId { get; set; }
    public int RowNumber { get; set; }
    public BulkUploadRowStatus Status { get; set; } = BulkUploadRowStatus.Pending;
    public Guid? ProductId { get; set; }
    public string? ErrorMessage { get; set; }

    public BulkUploadJob? Job { get; set; }
}
