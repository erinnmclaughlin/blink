namespace Blink.VideosApi.Contracts.Delete;

public sealed record DeleteVideoCommand : IRequest<DeleteVideoResponse>
{
    public required string BlobName { get; init; }
}

