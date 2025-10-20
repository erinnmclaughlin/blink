using Blink.Messaging;
using Blink.Storage;
using Blink.Videos;
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
        var video = context.Message.Video;

        _logger.LogInformation("Extracting metadata for video: {VideoBlobName}", video.File.BlobName);

        var metadata = await ExtractMetadataAsync(video.File.BlobName);

        if (metadata is not null)
        {
            await _publishEndpoint.Publish(new VideoMetadataExtracted
            {
                VideoId = video.Id,
                Metadata = metadata
            });
        }
    }

    private async Task<BlinkVideoMetaData?> ExtractMetadataAsync(string blobName)
    {
        _logger.LogInformation("Extracting metadata from uploaded video: {BlobName}", blobName);
        
        try
        {
            await using var videoStream = await _videoStorageClient.DownloadAsync(blobName);
            var metadata = await _videoMetadataExtractor.ExtractMetadataAsync(videoStream);

            if (metadata != null)
            {
                _logger.LogInformation("Video metadata extracted from {BlobName}", blobName);
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
