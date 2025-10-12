using Azure.Storage.Queues;

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
    ValueTask<string?> DequeueAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Azure Storage Queue implementation of thumbnail queue for reliable distributed processing
/// </summary>
public sealed class ThumbnailQueue : IThumbnailQueue
{
    private const string QueueName = "thumbnail-generation";
    private readonly QueueServiceClient _queueServiceClient;
    private readonly ILogger<ThumbnailQueue> _logger;
    private QueueClient? _queueClient;

    public ThumbnailQueue(
        QueueServiceClient queueServiceClient,
        ILogger<ThumbnailQueue> logger)
    {
        _queueServiceClient = queueServiceClient;
        _logger = logger;
    }

    private async Task<QueueClient> GetQueueClientAsync(CancellationToken cancellationToken)
    {
        if (_queueClient is not null)
        {
            return _queueClient;
        }

        _queueClient = _queueServiceClient.GetQueueClient(QueueName);
        await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        return _queueClient;
    }

    public async ValueTask EnqueueAsync(string videoBlobName, CancellationToken cancellationToken = default)
    {
        var queueClient = await GetQueueClientAsync(cancellationToken);
        await queueClient.SendMessageAsync(videoBlobName, cancellationToken: cancellationToken);
        _logger.LogInformation("Enqueued video for thumbnail generation: {VideoBlobName}", videoBlobName);
    }

    public async ValueTask<string?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        var queueClient = await GetQueueClientAsync(cancellationToken);
        var response = await queueClient.ReceiveMessageAsync(cancellationToken: cancellationToken);
        
        if (response.Value is null)
        {
            return null;
        }

        var message = response.Value;
        var videoBlobName = message.MessageText;
        
        // Delete the message from the queue after receiving it
        await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
        
        _logger.LogDebug("Dequeued video for thumbnail generation: {VideoBlobName}", videoBlobName);
        return videoBlobName;
    }
}
