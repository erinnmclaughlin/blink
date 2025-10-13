namespace Blink.VideosApi.Contracts.GetByBlobName;

public sealed record GetVideoByBlobNameQuery(string BlobName) : IRequest<VideoDetailDto>;