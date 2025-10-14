using Blink.Messaging;
using Blink.Storage;
using Blink.VideosApi.Contracts.CompleteUpload;
using MassTransit;
using MediatR;

namespace Blink.WebApi.Videos.CompleteUpload;

public sealed class CompleteUploadHandler : IRequestHandler<CompleteUploadRequest, CompleteUploadResponse>
{
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CompleteUploadHandler> _logger;
    private readonly IPublishEndpoint _videoEventPublisher;
    private readonly IVideoRepository _videoRepository;
    private readonly IVideoStorageClient _videoStorageClient;

    public CompleteUploadHandler(
        ICurrentUser currentUser,
        ILogger<CompleteUploadHandler> logger,
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

    public async Task<CompleteUploadResponse> Handle(CompleteUploadRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Completing upload for blob: {BlobName}", request.BlobName);

        // Get the actual file size from blob storage
        var fileSize = await _videoStorageClient.GetBlobSizeAsync(request.BlobName, cancellationToken);

        // Create database record
        var video = await SaveToDatabaseAsync(request, fileSize, cancellationToken);

        // Publish VideoUploaded event
        await PublishVideoUploadedEventAsync(video);

        _logger.LogInformation("Upload completed successfully for blob: {BlobName}", request.BlobName);

        return new CompleteUploadResponse
        {
            BlobName = request.BlobName,
            FileSize = fileSize,
            Success = true
        };
    }

    private async Task<Video> SaveToDatabaseAsync(CompleteUploadRequest request, long fileSize, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var video = new Video
        {
            Id = Guid.NewGuid(),
            BlobName = request.BlobName,
            Title = !string.IsNullOrWhiteSpace(request.Title)
                ? request.Title
                : Path.GetFileNameWithoutExtension(request.FileName),
            Description = request.Description,
            VideoDate = request.VideoDate,
            FileName = request.FileName,
            ContentType = GetContentType(request.FileName),
            SizeInBytes = fileSize,
            OwnerId = _currentUser.UserId,
            UploadedAt = now,
            UpdatedAt = now
        };

        await _videoRepository.CreateAsync(video, cancellationToken);

        _logger.LogInformation("Video saved to database: {BlobName}, Owner: {OwnerId}", request.BlobName, _currentUser.UserId);

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

