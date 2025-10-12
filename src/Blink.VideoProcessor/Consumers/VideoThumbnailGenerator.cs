using Blink.Messaging;
using Blink.Storage;
using MassTransit;

namespace Blink.VideoProcessor.Consumers;

public sealed class VideoThumbnailGenerator : IConsumer<VideoUploadedEvent>
{
    private readonly ILogger<VideoThumbnailGenerator> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly IVideoStorageClient _videoStorageClient;

    public VideoThumbnailGenerator(
        ILogger<VideoThumbnailGenerator> logger,
        IPublishEndpoint publishEndpoint,
        IThumbnailGenerator thumbnailGenerator,
        IVideoStorageClient videoStorageClient)
    {
        _logger = logger;
        _publishEndpoint = publishEndpoint;
        _thumbnailGenerator = thumbnailGenerator;
        _videoStorageClient = videoStorageClient;
    }

    public async Task Consume(ConsumeContext<VideoUploadedEvent> context)
    {
        var videoBlobName = context.Message.BlobName;

        _logger.LogInformation("Processing thumbnail generation for video: {VideoBlobName}", videoBlobName);

        // Download the video
        _logger.LogInformation("Downloading video for thumbnail generation: {VideoBlobName}", videoBlobName);
        using var videoStream = await _videoStorageClient.DownloadAsync(videoBlobName);

        // Generate thumbnail
        _logger.LogInformation("Generating thumbnail for video: {VideoBlobName}", videoBlobName);
        using var thumbnailStream = await _thumbnailGenerator.GenerateThumbnailAsync(videoStream);

        // Upload thumbnail
        _logger.LogInformation("Uploading thumbnail for video: {VideoBlobName}", videoBlobName);
        var thumbnailBlobName = await _videoStorageClient.UploadThumbnailAsync(thumbnailStream, videoBlobName);

        // Notify that thumbnail has been generated
        await _publishEndpoint.Publish(new VideoThumbnailGenerated
        {
            ThumbnailBlobName = thumbnailBlobName,
            VideoBlobName = videoBlobName
        });

        _logger.LogInformation("Successfully generated thumbnail for video: {VideoBlobName}, Thumbnail: {ThumbnailBlobName}", videoBlobName, thumbnailBlobName);
    }
}
