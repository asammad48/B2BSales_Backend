using System.Threading.Channels;

namespace B2BSpareParts.Infrastructure.Services.BulkUpload;

public interface IBulkUploadBackgroundQueue
{
    ValueTask QueueAsync(Guid jobId, CancellationToken ct = default);
    ValueTask<Guid> DequeueAsync(CancellationToken ct = default);
}

public class BulkUploadBackgroundQueue : IBulkUploadBackgroundQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    public ValueTask QueueAsync(Guid jobId, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(jobId, ct);

    public ValueTask<Guid> DequeueAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAsync(ct);
}
