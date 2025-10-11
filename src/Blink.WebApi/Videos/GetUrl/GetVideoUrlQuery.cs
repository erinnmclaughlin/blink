using MediatR;

namespace Blink.WebApi.Videos.GetUrl;

public sealed record GetVideoUrlQuery : IRequest<VideoUrlResponse>
{
    public required string BlobName { get; init; }
}

