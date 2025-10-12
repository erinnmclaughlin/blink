using Blink.Messaging;
using MassTransit;

namespace Blink.WebApi.Videos.Thumbnails;

/// <summary>
/// Background service that processes the thumbnail generation queue
/// </summary>
public sealed class ThumbnailGenerationService : IConsumer<VideoUploadedEvent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ThumbnailGenerationService> _logger;

    public ThumbnailGenerationService(
        IServiceProvider serviceProvider,
        ILogger<ThumbnailGenerationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<VideoUploadedEvent> context)
    {
        var videoBlobName = context.Message.BlobName;

        _logger.LogInformation("Processing thumbnail generation for video: {VideoBlobName}", videoBlobName);

        // Create a scope for this work item
        await using var scope = _serviceProvider.CreateAsyncScope();

        var videoStorageClient = scope.ServiceProvider.GetRequiredService<IVideoStorageClient>();
        var videoRepository = scope.ServiceProvider.GetRequiredService<IVideoRepository>();
        var thumbnailGenerator = scope.ServiceProvider.GetRequiredService<IThumbnailGenerator>();

        try
        {
            // Download the video
            _logger.LogInformation("Downloading video for thumbnail generation: {VideoBlobName}", videoBlobName);
            using var videoStream = await videoStorageClient.DownloadAsync(videoBlobName);

            // Generate thumbnail
            _logger.LogInformation("Generating thumbnail for video: {VideoBlobName}", videoBlobName);
            using var thumbnailStream = await thumbnailGenerator.GenerateThumbnailAsync(videoStream);

            // Upload thumbnail
            _logger.LogInformation("Uploading thumbnail for video: {VideoBlobName}", videoBlobName);
            var thumbnailBlobName = await videoStorageClient.UploadThumbnailAsync(thumbnailStream, videoBlobName);

            // Update video record with thumbnail
            _logger.LogInformation("Updating video record with thumbnail: {VideoBlobName}", videoBlobName);
            await videoRepository.UpdateThumbnailAsync(videoBlobName, thumbnailBlobName);

            _logger.LogInformation("Successfully generated and saved thumbnail for video: {VideoBlobName}, Thumbnail: {ThumbnailBlobName}", videoBlobName, thumbnailBlobName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing thumbnail generation for video: {VideoBlobName}", videoBlobName);
            // Continue processing other videos even if one fails
        }
    }
}
