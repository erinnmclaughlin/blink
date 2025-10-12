using MediatR;

namespace Blink.WebApi.Videos.GetUrl;

public sealed class GetVideoUrlQueryHandler : IRequestHandler<GetVideoUrlQuery, VideoUrlResponse>
{
    private readonly IVideoStorageClient _videoStorageClient;
    private readonly IVideoRepository _videoRepository;
    private readonly ILogger<GetVideoUrlQueryHandler> _logger;

    public GetVideoUrlQueryHandler(
        IVideoStorageClient videoStorageClient,
        IVideoRepository videoRepository,
        ILogger<GetVideoUrlQueryHandler> logger)
    {
        _videoStorageClient = videoStorageClient;
        _videoRepository = videoRepository;
        _logger = logger;
    }

    public async Task<VideoUrlResponse> Handle(GetVideoUrlQuery request, CancellationToken cancellationToken)
    {
        var url = await _videoStorageClient.GetUrlAsync(request.BlobName, cancellationToken);
        _logger.LogInformation("Generated URL for video: {BlobName}", request.BlobName);
        
        // Try to get thumbnail URL if available
        string? thumbnailUrl = null;
        var video = await _videoRepository.GetByBlobNameAsync(request.BlobName, cancellationToken);
        if (video?.ThumbnailBlobName != null)
        {
            thumbnailUrl = await _videoStorageClient.GetThumbnailUrlAsync(video.ThumbnailBlobName, cancellationToken);
        }
        
        return new VideoUrlResponse 
        { 
            Url = url,
            ThumbnailUrl = thumbnailUrl
        };
    }
}
