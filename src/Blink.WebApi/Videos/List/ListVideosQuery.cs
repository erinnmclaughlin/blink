using Blink.Storage;
using MediatR;

namespace Blink.WebApi.Videos.List;

public sealed record ListVideosQuery : IRequest<List<VideoInfo>>
{
}
