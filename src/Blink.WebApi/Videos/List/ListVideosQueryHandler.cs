using MediatR;
using System.Security.Claims;

namespace Blink.WebApi.Videos.List;

public sealed class ListVideosQueryHandler : IRequestHandler<ListVideosQuery, List<VideoInfo>>
{
    private readonly IVideoRepository _videoRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ListVideosQueryHandler> _logger;

    public ListVideosQueryHandler(
        IVideoRepository videoRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ListVideosQueryHandler> logger)
    {
        _videoRepository = videoRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<List<VideoInfo>> Handle(ListVideosQuery request, CancellationToken cancellationToken)
    {
        // Get the current user's ID from claims
        var userId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("User ID not found in claims");

        // Get videos for the current user
        var videos = await _videoRepository.GetByOwnerIdAsync(userId, cancellationToken);
        
        // Convert Video entities to VideoInfo DTOs
        var videoInfoList = videos.Select(v => new VideoInfo(
            BlobName: v.BlobName,
            FileName: v.FileName,
            SizeInBytes: v.SizeInBytes,
            LastModified: new DateTimeOffset(v.UploadedAt, TimeSpan.Zero),
            ContentType: v.ContentType,
            Title: v.Title
        )).ToList();

        _logger.LogInformation("Retrieved {Count} videos for user {UserId}", videoInfoList.Count, userId);
        return videoInfoList;
    }
}
