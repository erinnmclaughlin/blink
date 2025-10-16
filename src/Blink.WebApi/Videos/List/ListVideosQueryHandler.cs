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
        var dtos = videos.Select(v => new VideoSummaryDto
        {
            Title = v.Title,
            Description = v.Description,
            DurationInSeconds = v.DurationInSeconds,
            VideoDate = v.VideoDate is null ? null : DateOnly.FromDateTime(v.VideoDate.Value),
            SizeInBytes = v.SizeInBytes,
            ThumbnailBlobName = v.ThumbnailBlobName,
            VideoBlobName = v.BlobName,
            UploadedAt = DateOnly.FromDateTime(v.UploadedAt)
        });

        return [.. dtos];
    }
}
