using Blink.WebApi.Videos.Thumbnails;
using MediatR;

namespace Blink.WebApi.Videos.Upload;

public sealed class UploadVideoRequestHandler : IRequestHandler<UploadVideoRequest, UploadedVideoInfo>
{
    private readonly ICurrentUser _currentUser;
    private readonly IVideoStorageClient _videoStorageClient;
    private readonly IVideoRepository _videoRepository;
    private readonly IThumbnailQueue _thumbnailQueue;
    private readonly IVideoMetadataExtractor _metadataExtractor;
    private readonly ILogger<UploadVideoRequestHandler> _logger;

    public UploadVideoRequestHandler(
        ICurrentUser currentUser,
        IVideoStorageClient videoStorageClient,
        IVideoRepository videoRepository,
        IThumbnailQueue thumbnailQueue,
        IVideoMetadataExtractor metadataExtractor,
        ILogger<UploadVideoRequestHandler> logger)
    {
        _currentUser = currentUser;
        _videoStorageClient = videoStorageClient;
        _videoRepository = videoRepository;
        _thumbnailQueue = thumbnailQueue;
        _metadataExtractor = metadataExtractor;
        _logger = logger;
    }

    public async Task<UploadedVideoInfo> Handle(UploadVideoRequest request, CancellationToken cancellationToken)
    {
        // Upload to blob storage first
        using var stream = request.File.OpenReadStream();
        var (blobName, fileSize) = await _videoStorageClient.UploadAsync(stream, request.File.FileName, cancellationToken);

        _logger.LogInformation("Video uploaded to blob storage: {BlobName}", blobName);

        // Extract metadata from uploaded video
        VideoMetadata? metadata = null;
        try
        {
            _logger.LogInformation("Extracting metadata from uploaded video: {BlobName}", blobName);
            using var videoStream = await _videoStorageClient.DownloadAsync(blobName, cancellationToken);
            metadata = await _metadataExtractor.ExtractMetadataAsync(videoStream, cancellationToken);
            
            if (metadata != null)
            {
                _logger.LogInformation("Video metadata extracted: {Width}x{Height} for {BlobName}", 
                    metadata.Width, metadata.Height, blobName);
            }
            else
            {
                _logger.LogWarning("Could not extract metadata from video: {BlobName}", blobName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting metadata from video: {BlobName}", blobName);
            // Continue with database save even if metadata extraction fails
        }

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
            Height = metadata?.Height
        };

        await _videoRepository.CreateAsync(video, cancellationToken);

        _logger.LogInformation("Video saved to database: {BlobName}, Owner: {OwnerId}, Dimensions: {Width}x{Height}", 
            blobName, _currentUser.UserId, metadata?.Width, metadata?.Height);

        // Queue video for thumbnail generation
        await _thumbnailQueue.EnqueueAsync(blobName, cancellationToken);
        _logger.LogInformation("Video queued for thumbnail generation: {BlobName}", blobName);

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
}