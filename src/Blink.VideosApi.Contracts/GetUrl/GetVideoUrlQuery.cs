namespace Blink.VideosApi.Contracts.GetUrl;

public sealed record GetVideoUrlQuery : IRequest<VideoUrlResponse>
{
    public required string BlobName { get; init; }
}
