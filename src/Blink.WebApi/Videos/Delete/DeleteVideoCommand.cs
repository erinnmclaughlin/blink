using MediatR;

namespace Blink.WebApi.Videos.Delete;

public sealed record DeleteVideoCommand : IRequest<DeleteVideoResponse>
{
    public required string BlobName { get; init; }
}

