using Blink.Messaging;
using Blink.Storage;
using MassTransit;
using MediatR;

namespace Blink.WebApi.Videos.Upload;

public sealed class UploadVideoRequestHandler : IRequestHandler<UploadVideoRequest, UploadedVideoInfo>
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UploadVideoRequestHandler> _logger;
    private readonly IPublishEndpoint _videoEventPublisher;
    private readonly IVideoRepository _videoRepository;
    private readonly IVideoStorageClient _videoStorageClient;

    public UploadVideoRequestHandler(
        ICurrentUser currentUser,
        ILogger<UploadVideoRequestHandler> logger,
        IPublishEndpoint videoEventPublisher,
        IVideoRepository videoRepository,
        IVideoStorageClient videoStorageClient)
    {
        _currentUser = currentUser;
        _logger = logger;
        _videoEventPublisher = videoEventPublisher;
        _videoRepository = videoRepository;
        _videoStorageClient = videoStorageClient;
    }

    public async Task<UploadedVideoInfo> Handle(UploadVideoRequest request, CancellationToken cancellationToken)
    {
        // Upload to blob storage first
        var (blobName, fileSize) = await UploadVideoAsync(request.File, cancellationToken);

        // Create database record
        var video = await SaveToDatabaseAsync(blobName, fileSize, request, cancellationToken);

        // Publish VideoUploaded event (don't pass cancellationToken to avoid cancellation after client disconnect)
        await PublishVideoUploadedEventAsync(video);

        return new UploadedVideoInfo
        {
            BlobName = blobName,
            FileName = request.File.FileName,
            FileSize = fileSize
        };
    }

    private async Task<(string BlobName, long FileSize)> UploadVideoAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using var stream = file.OpenReadStream();
        var info = await _videoStorageClient.UploadAsync(stream, file.FileName, cancellationToken);

        _logger.LogInformation("Video uploaded to blob storage: {BlobName}", info.BlobName);

        return info;
    }

    private async Task<Video> SaveToDatabaseAsync(string blobName, long fileSize, UploadVideoRequest request, CancellationToken cancellationToken)
    {
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
            ContentType = GetContentType(request.File.FileName),
            SizeInBytes = fileSize,
            OwnerId = _currentUser.UserId,
            UploadedAt = now,
            UpdatedAt = now
        };

        await _videoRepository.CreateAsync(video, cancellationToken);

        _logger.LogInformation("Video saved to database: {BlobName}, Owner: {OwnerId}", blobName, _currentUser.UserId);

        return video;
    }

    private async Task PublishVideoUploadedEventAsync(Video video)
    {
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
            UploadedAt = video.UploadedAt
        };

        // Use CancellationToken.None to ensure message is published even if HTTP request is cancelled
        // This prevents "task was canceled" errors when the client disconnects after receiving the response
        await _videoEventPublisher.Publish(videoUploadedEvent, CancellationToken.None);
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