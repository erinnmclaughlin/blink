namespace Blink.WebApi.Videos.Thumbnails;

/// <summary>
/// Background service that processes the thumbnail generation queue
/// </summary>
public sealed class ThumbnailGenerationService : BackgroundService
{
    private readonly IThumbnailQueue _thumbnailQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ThumbnailGenerationService> _logger;

    public ThumbnailGenerationService(
        IThumbnailQueue thumbnailQueue,
        IServiceProvider serviceProvider,
        ILogger<ThumbnailGenerationService> logger)
    {
        _thumbnailQueue = thumbnailQueue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Thumbnail Generation Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Dequeue the next video for processing
                var videoBlobName = await _thumbnailQueue.DequeueAsync(stoppingToken);

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
                    using var videoStream = await videoStorageClient.DownloadAsync(videoBlobName, stoppingToken);

                    // Generate thumbnail
                    _logger.LogInformation("Generating thumbnail for video: {VideoBlobName}", videoBlobName);
                    using var thumbnailStream = await thumbnailGenerator.GenerateThumbnailAsync(videoStream, stoppingToken);

                    // Upload thumbnail
                    _logger.LogInformation("Uploading thumbnail for video: {VideoBlobName}", videoBlobName);
                    var thumbnailBlobName = await videoStorageClient.UploadThumbnailAsync(thumbnailStream, videoBlobName, stoppingToken);

                    // Update video record with thumbnail
                    _logger.LogInformation("Updating video record with thumbnail: {VideoBlobName}", videoBlobName);
                    await videoRepository.UpdateThumbnailAsync(videoBlobName, thumbnailBlobName, stoppingToken);

                    _logger.LogInformation("Successfully generated and saved thumbnail for video: {VideoBlobName}, Thumbnail: {ThumbnailBlobName}", 
                        videoBlobName, thumbnailBlobName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing thumbnail generation for video: {VideoBlobName}", videoBlobName);
                    // Continue processing other videos even if one fails
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
                _logger.LogInformation("Thumbnail Generation Service stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Thumbnail Generation Service");
                // Wait a bit before continuing to avoid tight error loops
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        _logger.LogInformation("Thumbnail Generation Service stopped");
    }
}

