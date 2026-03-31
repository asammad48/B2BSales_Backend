using B2BSpareParts.Domain.Common;

namespace B2BSpareParts.Domain.Entities.BulkUpload;

public enum BulkUploadJobStatus
{
    Pending = 1,
    Processing = 2,
    Completed = 3,
    Failed = 4
}

public class BulkUploadJob : TenantEntity
{
    public string FilePath { get; set; } = default!;
    public BulkUploadJobStatus Status { get; set; } = BulkUploadJobStatus.Pending;
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
