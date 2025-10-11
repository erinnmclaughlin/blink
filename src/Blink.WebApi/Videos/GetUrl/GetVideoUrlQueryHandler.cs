using MediatR;

namespace Blink.WebApi.Videos.GetUrl;

public sealed class GetVideoUrlQueryHandler : IRequestHandler<GetVideoUrlQuery, VideoUrlResponse>
{
    private readonly IVideoStorageClient _videoStorageClient;
    private readonly ILogger<GetVideoUrlQueryHandler> _logger;

    public GetVideoUrlQueryHandler(IVideoStorageClient videoStorageClient, ILogger<GetVideoUrlQueryHandler> logger)
    {
        _videoStorageClient = videoStorageClient;
        _logger = logger;
    }

    public async Task<VideoUrlResponse> Handle(GetVideoUrlQuery request, CancellationToken cancellationToken)
    {
        var url = await _videoStorageClient.GetUrlAsync(request.BlobName, cancellationToken);
        _logger.LogInformation("Generated URL for video: {BlobName}", request.BlobName);
        
        return new VideoUrlResponse { Url = url };
    }
}
