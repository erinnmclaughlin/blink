using Blink.WebApi.Videos.Events;
using Blink.WebApi.Videos.Thumbnails;
using MassTransit;
using MediatR;

namespace Blink.WebApi.Videos.Upload;

public sealed class UploadVideoRequestHandler : IRequestHandler<UploadVideoRequest, UploadedVideoInfo>
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UploadVideoRequestHandler> _logger;
    private readonly IVideoMetadataExtractor _metadataExtractor;
    private readonly IThumbnailQueue _thumbnailQueue;
    private readonly IPublishEndpoint _videoEventPublisher;
    private readonly IVideoRepository _videoRepository;
    private readonly IVideoStorageClient _videoStorageClient;

    public UploadVideoRequestHandler(
        ICurrentUser currentUser,
        ILogger<UploadVideoRequestHandler> logger,
        IVideoMetadataExtractor metadataExtractor,
        IThumbnailQueue thumbnailQueue,
        IPublishEndpoint videoEventPublisher,
        IVideoRepository videoRepository,
        IVideoStorageClient videoStorageClient)
    {
        _currentUser = currentUser;
        _logger = logger;
        _metadataExtractor = metadataExtractor;
        _thumbnailQueue = thumbnailQueue;
        _videoEventPublisher = videoEventPublisher;
        _videoRepository = videoRepository;
        _videoStorageClient = videoStorageClient;
    }

    public async Task<UploadedVideoInfo> Handle(UploadVideoRequest request, CancellationToken cancellationToken)
    {
        // Upload to blob storage first
        using var stream = request.File.OpenReadStream();
        var (blobName, fileSize) = await _videoStorageClient.UploadAsync(stream, request.File.FileName, cancellationToken);

        _logger.LogInformation("Video uploaded to blob storage: {BlobName}", blobName);

        // Extract metadata from uploaded video
        var metadata = await ExtractMetadataAsync(blobName, cancellationToken);

        // Get content type
        var contentType = GetContentType(request.File.FileName);

        // Create database record
        var now = DateTime.UtcNow;
        var video = new Video
        {
            Id = Guid.NewGuid(),
            BlobName = blobName,
            Title = !string.IsNullOrWhiteSpace(request.Title) 
                ? request.Title 
                : Path.GetFileNameWithoutExtension(request.File.FileName), // Default title from filename
            Description = request.Description,
            VideoDate = request.VideoDate,
            FileName = request.File.FileName,
            ContentType = contentType,
            SizeInBytes = fileSize,
            OwnerId = _currentUser.UserId,
            UploadedAt = now,
            UpdatedAt = now,
            Width = metadata?.Width,
            Height = metadata?.Height,
            DurationInSeconds = metadata?.DurationInSeconds
        };

        await _videoRepository.CreateAsync(video, cancellationToken);

        _logger.LogInformation("Video saved to database: {BlobName}, Owner: {OwnerId}, Dimensions: {Width}x{Height}, Duration: {Duration}s", 
            blobName, _currentUser.UserId, metadata?.Width, metadata?.Height, metadata?.DurationInSeconds);

        // Queue video for thumbnail generation
        //await _thumbnailQueue.EnqueueAsync(blobName, cancellationToken);
        //_logger.LogInformation("Video queued for thumbnail generation: {BlobName}", blobName);

        // Publish VideoUploaded event to Service Bus
        var videoUploadedEvent = new VideoUploadedEvent
        {
            VideoId = video.Id,
            BlobName = video.BlobName,
            Title = video.Title,
            Description = video.Description,
            OwnerId = video.OwnerId,
            FileName = video.FileName,
            ContentType = video.ContentType,
            SizeInBytes = video.SizeInBytes,
            UploadedAt = video.UploadedAt,
            Width = video.Width,
            Height = video.Height,
            DurationInSeconds = video.DurationInSeconds
        };

        await _videoEventPublisher.Publish(videoUploadedEvent, cancellationToken);

        /*var sender = _serviceBus.CreateSender(ServiceNames.ServiceBusVideosTopic);
        await sender.SendMessageAsync(new ServiceBusMessage(BinaryData.FromString(JsonSerializer.Serialize(videoUploadedEvent)))
        {
            Subject = nameof(VideoUploadedEvent),
            ContentType = "application/json"
        }, cancellationToken);*/

        return new UploadedVideoInfo
        {
            BlobName = blobName,
            FileName = request.File.FileName,
            FileSize = fileSize
        };
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".avi" => "video/x-msvideo",
            ".wmv" => "video/x-ms-wmv",
            _ => "application/octet-stream"
        };
    }

    private async Task<VideoMetadata?> ExtractMetadataAsync(string blobName, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Extracting metadata from uploaded video: {BlobName}", blobName);
            using var videoStream = await _videoStorageClient.DownloadAsync(blobName, cancellationToken);
            var metadata = await _metadataExtractor.ExtractMetadataAsync(videoStream, cancellationToken);

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