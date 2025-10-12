using Blink.WebApi.Videos.Thumbnails;
using MediatR;

namespace Blink.WebApi.Videos.Upload;

public sealed class UploadVideoRequestHandler : IRequestHandler<UploadVideoRequest, UploadedVideoInfo>
{
    private readonly ICurrentUser _currentUser;
    private readonly IVideoStorageClient _videoStorageClient;
    private readonly IVideoRepository _videoRepository;
    private readonly IThumbnailQueue _thumbnailQueue;
    private readonly ILogger<UploadVideoRequestHandler> _logger;

    public UploadVideoRequestHandler(
        ICurrentUser currentUser,
        IVideoStorageClient videoStorageClient,
        IVideoRepository videoRepository,
        IThumbnailQueue thumbnailQueue,
        ILogger<UploadVideoRequestHandler> logger)
    {
        _currentUser = currentUser;
        _videoStorageClient = videoStorageClient;
        _videoRepository = videoRepository;
        _thumbnailQueue = thumbnailQueue;
        _logger = logger;
    }

    public async Task<UploadedVideoInfo> Handle(UploadVideoRequest request, CancellationToken cancellationToken)
    {
        // Upload to blob storage
        using var stream = request.File.OpenReadStream();
        var (blobName, fileSize) = await _videoStorageClient.UploadAsync(stream, request.File.FileName, cancellationToken);

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
            UpdatedAt = now
        };

        await _videoRepository.CreateAsync(video, cancellationToken);

        _logger.LogInformation("Video uploaded and saved to database: {BlobName}, Owner: {OwnerId}", blobName, _currentUser.UserId);

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