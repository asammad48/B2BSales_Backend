using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace B2BSpareParts.Infrastructure.Services.BulkUpload;

public class BulkProductUploadBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BulkProductUploadBackgroundService> _logger;
    private readonly IBulkUploadBackgroundQueue _queue;

    public BulkProductUploadBackgroundService(
        IServiceScopeFactory serviceScopeFactory,
        ILogger<BulkProductUploadBackgroundService> logger,
        IBulkUploadBackgroundQueue queue)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var jobId = await _queue.DequeueAsync(stoppingToken);

            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<BulkProductUploadProcessor>();
                await processor.ProcessAsync(jobId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bulk upload job {JobId} crashed at background worker level.", jobId);
            }
        }
    }
}
