using MediatR;

namespace Blink.WebApi.Videos.List;

public sealed class ListVideosQueryHandler : IRequestHandler<ListVideosQuery, List<VideoInfo>>
{
    private readonly IVideoStorageClient _videoStorageClient;
    private readonly ILogger<ListVideosQueryHandler> _logger;

    public ListVideosQueryHandler(IVideoStorageClient videoStorageClient, ILogger<ListVideosQueryHandler> logger)
    {
        _videoStorageClient = videoStorageClient;
        _logger = logger;
    }

    public async Task<List<VideoInfo>> Handle(ListVideosQuery request, CancellationToken cancellationToken)
    {
        var videos = await _videoStorageClient.ListAsync(cancellationToken);
        _logger.LogInformation("Retrieved {Count} videos", videos.Count);
        return videos;
    }
}
