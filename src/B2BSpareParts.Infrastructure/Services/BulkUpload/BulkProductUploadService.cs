using B2BSpareParts.Application.Common;
using B2BSpareParts.Application.Contracts;
using B2BSpareParts.Application.DTOs.BulkUpload;
using B2BSpareParts.Domain.Entities.BulkUpload;
using B2BSpareParts.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace B2BSpareParts.Infrastructure.Services.BulkUpload;

public class BulkProductUploadService : IBulkProductUploadService
{
    private readonly AppDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly IAppEnvironment _appEnvironment;
    private readonly IConfiguration _configuration;
    private readonly IBulkUploadBackgroundQueue _queue;

    public BulkProductUploadService(
        AppDbContext db,
        ITenantContext tenantContext,
        IAppEnvironment appEnvironment,
        IConfiguration configuration,
        IBulkUploadBackgroundQueue queue)
    {
        _db = db;
        _tenantContext = tenantContext;
        _appEnvironment = appEnvironment;
        _configuration = configuration;
        _queue = queue;
    }

    public async Task<Guid> CreateJobAsync(IFormFile file, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new AppException("CSV file is required.", 400);

        if (file.Length > 30 * 1024 * 1024)
            throw new AppException("File size exceeds 30MB limit.", 400);

        var tenantId = _tenantContext.TenantId;
        if (tenantId == Guid.Empty)
            throw new AppException("Tenant context is required.", 400);

        var uploadFolder = _configuration["FileStorage:UploadFolder"] ?? "uploads";
        var bulkFolder = Path.Combine(_appEnvironment.ContentRootPath, uploadFolder, "bulk");
        Directory.CreateDirectory(bulkFolder);

        var fileName = $"bulk-{Guid.NewGuid()}.csv";
        var relativePath = Path.Combine(uploadFolder, "bulk", fileName).Replace('\\', '/');
        var fullPath = Path.Combine(bulkFolder, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
        {
            await file.CopyToAsync(stream, ct);
        }

        var job = new BulkUploadJob
        {
            TenantId = tenantId,
            FilePath = relativePath,
            Status = BulkUploadJobStatus.Pending
        };

        _db.BulkUploadJobs.Add(job);
        await _db.SaveChangesAsync(ct);
        return job.Id;
    }

    public async Task EnqueueJobAsync(Guid jobId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var jobExists = await _db.BulkUploadJobs.AnyAsync(x => x.Id == jobId && x.TenantId == tenantId && !x.IsDeleted, ct);
        if (!jobExists)
            throw new AppException("Bulk upload job not found.", 404);

        await _queue.QueueAsync(jobId, ct);
    }

    public async Task<BulkProductUploadStatusResponseDto> GetStatusAsync(Guid jobId, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var job = await _db.BulkUploadJobs.FirstOrDefaultAsync(x => x.Id == jobId && x.TenantId == tenantId && !x.IsDeleted, ct)
                  ?? throw new AppException("Bulk upload job not found.", 404);

        return new BulkProductUploadStatusResponseDto
        {
            JobId = job.Id,
            Status = job.Status.ToString(),
            TotalRows = job.TotalRows,
            ProcessedRows = job.ProcessedRows,
            SuccessfulRows = job.SuccessfulRows,
            FailedRows = job.FailedRows,
            ErrorMessage = job.ErrorMessage,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt
        };
    }
}
