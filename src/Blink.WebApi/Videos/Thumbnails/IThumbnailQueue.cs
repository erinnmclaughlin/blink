using System.Threading.Channels;

namespace Blink.WebApi.Videos.Thumbnails;

/// <summary>
/// Queue for processing video thumbnail generation requests
/// </summary>
public interface IThumbnailQueue
{
    /// <summary>
    /// Enqueues a video for thumbnail generation
    /// </summary>
    ValueTask EnqueueAsync(string videoBlobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeues a video for thumbnail generation
    /// </summary>
    ValueTask<string> DequeueAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Channel-based implementation of thumbnail queue for high-performance async processing
/// </summary>
public sealed class ThumbnailQueue : IThumbnailQueue
{
    private readonly Channel<string> _channel;
    private readonly ILogger<ThumbnailQueue> _logger;

    public ThumbnailQueue(ILogger<ThumbnailQueue> logger)
    {
        _logger = logger;
        // Create unbounded channel for simplicity
        // In production, consider bounded channel with appropriate capacity
        _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = false, // Allow multiple background workers if needed
            SingleWriter = false
        });
    }

    public async ValueTask EnqueueAsync(string videoBlobName, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(videoBlobName, cancellationToken);
        _logger.LogInformation("Enqueued video for thumbnail generation: {VideoBlobName}", videoBlobName);
    }

    public async ValueTask<string> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var videoBlobName = await _channel.Reader.ReadAsync(cancellationToken);
        _logger.LogDebug("Dequeued video for thumbnail generation: {VideoBlobName}", videoBlobName);
        return videoBlobName;
    }
}

