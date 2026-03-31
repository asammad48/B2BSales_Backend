using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.BulkUpload;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace B2BSpareParts.Api.Controllers;

[ApiController]
[Route("api/bulk-product-upload")]
[Authorize]
public class BulkProductUploadController : ControllerBase
{
    private readonly IBulkProductUploadService _bulkUploadService;

    public BulkProductUploadController(IBulkProductUploadService bulkUploadService)
    {
        _bulkUploadService = bulkUploadService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<Guid>>> Upload(IFormFile file, CancellationToken ct)
    {
        var jobId = await _bulkUploadService.CreateJobAsync(file, ct);
        await _bulkUploadService.EnqueueJobAsync(jobId, ct);
        return Ok(ApiResponse<Guid>.Ok(jobId, "Bulk upload queued."));
    }

    [HttpPost("{jobId:guid}/resume")]
    public async Task<ActionResult<ApiResponse<Guid>>> Resume(Guid jobId, CancellationToken ct)
    {
        await _bulkUploadService.EnqueueJobAsync(jobId, ct);
        return Ok(ApiResponse<Guid>.Ok(jobId, "Bulk upload resume queued."));
    }

    [HttpGet("{jobId:guid}/status")]
    public async Task<ActionResult<ApiResponse<BulkProductUploadStatusResponseDto>>> Status(Guid jobId, CancellationToken ct)
        => Ok(ApiResponse<BulkProductUploadStatusResponseDto>.Ok(await _bulkUploadService.GetStatusAsync(jobId, ct)));
}
