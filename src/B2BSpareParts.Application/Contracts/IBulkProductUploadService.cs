using B2BSpareParts.Application.DTOs.BulkUpload;
using Microsoft.AspNetCore.Http;

namespace B2BSpareParts.Application.Contracts;

public interface IBulkProductUploadService
{
    Task<Guid> CreateJobAsync(IFormFile file, CancellationToken ct = default);
    Task EnqueueJobAsync(Guid jobId, CancellationToken ct = default);
    Task<BulkProductUploadStatusResponseDto> GetStatusAsync(Guid jobId, CancellationToken ct = default);
}
