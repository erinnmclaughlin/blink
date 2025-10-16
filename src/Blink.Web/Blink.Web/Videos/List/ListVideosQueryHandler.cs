using Blink.VideosApi.Contracts.List;
using MediatR;

namespace Blink.Web.Videos.List;

internal sealed class ListVideosQueryHandler : IRequestHandler<ListVideosQuery, List<VideoSummaryDto>>
{
    private readonly IVideoRepository _videoRepository;

    public ListVideosQueryHandler(IVideoRepository videoRepository)
    {
        _videoRepository = videoRepository;
    }

    public async Task<List<VideoSummaryDto>> Handle(ListVideosQuery request, CancellationToken cancellationToken)
    {
        var videos = await _videoRepository.GetAllAsync(cancellationToken);

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

        return [..dtos];
    }
}
