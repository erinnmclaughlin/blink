using Blink.Messaging;
using Blink.Storage;
using MassTransit;

namespace Blink.VideoMetadataExtractor;

public sealed class VideoUploadedEventConsumer : IConsumer<VideoUploadedEvent>
{
    private readonly ILogger<VideoUploadedEventConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IVideoMetadataExtractor _videoMetadataExtractor;
    private readonly IVideoStorageClient _videoStorageClient;

    public VideoUploadedEventConsumer(
        ILogger<VideoUploadedEventConsumer> logger,
        IPublishEndpoint publishEndpoint,
        IVideoMetadataExtractor videoMetadataExtractor,
        IVideoStorageClient videoStorageClient)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _videoMetadataExtractor = videoMetadataExtractor;
        _videoStorageClient = videoStorageClient;
    }

    public async Task Consume(ConsumeContext<VideoUploadedEvent> context)
    {
        var videoBlobName = context.Message.BlobName;

        _logger.LogInformation("Extracting metadata for video: {VideoBlobName}", videoBlobName);

        var metadata = await ExtractMetadataAsync(videoBlobName);

        if (metadata is not null)
        {
            await _publishEndpoint.Publish(new VideoMetadataExtracted
            {
                VideoBlobName = videoBlobName,
                Metadata = metadata
            });
        }
    }

    private async Task<VideoMetadata?> ExtractMetadataAsync(string blobName)
    {
        try
        {
            _logger.LogInformation("Extracting metadata from uploaded video: {BlobName}", blobName);
            using var videoStream = await _videoStorageClient.DownloadAsync(blobName);
            var metadata = await _videoMetadataExtractor.ExtractMetadataAsync(videoStream);

            if (metadata != null)
            {
                _logger.LogInformation("Video metadata extracted: {Width}x{Height}, Duration: {Duration}s for {BlobName}",
                    metadata.Width, metadata.Height, metadata.DurationInSeconds, blobName);
            }
            else
            {
                _logger.LogWarning("Could not extract metadata from video: {BlobName}", blobName);
            }

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting metadata from video: {BlobName}", blobName);
            // Continue with database save even if metadata extraction fails
            return null;
        }
    }
}
