namespace Blink.VideosApi.Contracts.GetById;

public sealed record GetVideoByIdQuery(Guid Id) : IRequest<VideoDetailDto?>;

