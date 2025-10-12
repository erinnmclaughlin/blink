using MediatR;

namespace Blink.WebApi.Videos.List;

public sealed class ListVideosQueryHandler : IRequestHandler<ListVideosQuery, List<VideoInfo>>
{
    private readonly ICurrentUser _currentUser;
    private readonly IVideoRepository _videoRepository;
    private readonly ILogger<ListVideosQueryHandler> _logger;

    public ListVideosQueryHandler(
        ICurrentUser currentUser,
        IVideoRepository videoRepository,
        ILogger<ListVideosQueryHandler> logger)
    {
        _currentUser = currentUser;
        _videoRepository = videoRepository;
        _logger = logger;
    }

    public async Task<List<VideoInfo>> Handle(ListVideosQuery request, CancellationToken cancellationToken)
    {
        // Get videos for the current user
        var videos = await _videoRepository.GetByOwnerIdAsync(_currentUser.UserId, cancellationToken);
        
        // Convert Video entities to VideoInfo DTOs
        var videoInfoList = videos.Select(v => new VideoInfo(
            BlobName: v.BlobName,
            FileName: v.FileName,
            SizeInBytes: v.SizeInBytes,
            LastModified: new DateTimeOffset(v.UploadedAt, TimeSpan.Zero),
            ContentType: v.ContentType,
            Title: v.Title,
            Description: v.Description,
            VideoDate: v.VideoDate,
            OwnerId: v.OwnerId,
            ThumbnailBlobName: v.ThumbnailBlobName
        )).ToList();

        _logger.LogInformation("Retrieved {Count} videos for user {UserId}", videoInfoList.Count, _currentUser.UserId);
        return videoInfoList;
    }
}
