namespace Blink.VideosApi.Contracts.Delete;

public sealed record DeleteVideoResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public required string BlobName { get; init; }
}
