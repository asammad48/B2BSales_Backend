namespace B2BSpareParts.Application.DTOs.BulkUpload;

public class BulkProductUploadStatusResponseDto
{
    public Guid JobId { get; set; }
    public string Status { get; set; } = default!;
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
