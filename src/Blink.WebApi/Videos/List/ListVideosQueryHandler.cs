using Blink.VideosApi.Contracts.List;
using MediatR;

namespace Blink.WebApi.Videos.List;

public sealed class ListVideosQueryHandler : IRequestHandler<ListVideosQuery, List<VideoSummaryDto>>
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

    public async Task<List<VideoSummaryDto>> Handle(ListVideosQuery request, CancellationToken cancellationToken)
    {
        // Get videos for the current user
        var videos = await _videoRepository.GetByOwnerIdAsync(_currentUser.UserId, cancellationToken);

        // Convert Video entities to VideoInfo DTOs
        var videoInfoList = videos
            .Select(v => new VideoSummaryDto
            {
                Title = v.Title,
                Description = v.Description,
                VideoDate = v.VideoDate,
                ThumbnailBlobName = v.ThumbnailBlobName,
                VideoBlobName = v.BlobName
            })
            .ToList();

        _logger.LogInformation("Retrieved {Count} videos for user {UserId}", videoInfoList.Count, _currentUser.UserId);
        return videoInfoList;
    }
}
