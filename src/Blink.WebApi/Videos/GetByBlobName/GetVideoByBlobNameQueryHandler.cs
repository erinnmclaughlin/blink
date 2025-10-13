using Blink.Storage;
using Blink.VideosApi.Contracts.GetByBlobName;
using MediatR;

namespace Blink.WebApi.Videos.GetByBlobName;

public sealed class GetVideoByBlobNameQueryHandler : IRequestHandler<GetVideoByBlobNameQuery, VideoDetailDto>
{
    private readonly IVideoRepository _videoRepository;
    private readonly IVideoStorageClient _videoStorageClient;

    public GetVideoByBlobNameQueryHandler(IVideoRepository videoRepository, IVideoStorageClient videoStorageClient)
    {
        _videoRepository = videoRepository;
        _videoStorageClient = videoStorageClient;
    }

    public async Task<VideoDetailDto> Handle(GetVideoByBlobNameQuery request, CancellationToken cancellationToken)
    {
        var video = await _videoRepository.GetByBlobNameAsync(request.BlobName, cancellationToken);

        if (video is null)
        {
            throw new KeyNotFoundException($"Video with blob name '{request.BlobName}' not found.");
        }

        return new VideoDetailDto
        {
            Title = video.Title,
            Description = video.Description,
            VideoDate = video.VideoDate,
            ThumbnailBlobName = video.ThumbnailBlobName,
            VideoBlobName = video.BlobName,
            VideoWatchUrl = await _videoStorageClient.GetUrlAsync(video.BlobName, cancellationToken)
        };
    }
}
