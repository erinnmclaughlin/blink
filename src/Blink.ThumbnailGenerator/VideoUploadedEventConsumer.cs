using Blink.Messaging;
using Blink.Storage;
using MassTransit;

namespace Blink.ThumbnailGenerator;

public sealed class VideoUploadedEventConsumer : IConsumer<VideoUploadedEvent>
{
    private readonly ILogger<VideoUploadedEventConsumer> _logger;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly IThumbnailGenerator _thumbnailGenerator;
    private readonly IVideoStorageClient _videoStorageClient;

    public VideoUploadedEventConsumer(
        ILogger<VideoUploadedEventConsumer> logger,
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
        var blobName = context.Message.Video.File.BlobName;

        _logger.LogInformation("Processing thumbnail generation for video: {VideoBlobName}", blobName);

        // Download the video
        _logger.LogInformation("Downloading video for thumbnail generation: {VideoBlobName}", blobName);
        using var videoStream = await _videoStorageClient.DownloadAsync(blobName);

        // Generate thumbnail
        _logger.LogInformation("Generating thumbnail for video: {VideoBlobName}", blobName);
        using var thumbnailStream = await _thumbnailGenerator.GenerateThumbnailAsync(videoStream);

        // Upload thumbnail
        _logger.LogInformation("Uploading thumbnail for video: {VideoBlobName}", blobName);
        var thumbnailBlobName = await _videoStorageClient.UploadThumbnailAsync(thumbnailStream, blobName);

        // Notify that thumbnail has been generated
        await _publishEndpoint.Publish(new VideoThumbnailGenerated
        {
            VideoId = context.Message.Video.Id,
            ThumbnailBlobName = thumbnailBlobName
        });

        _logger.LogInformation("Successfully generated thumbnail for video: {VideoBlobName}, Thumbnail: {ThumbnailBlobName}", blobName, thumbnailBlobName);
    }
}
