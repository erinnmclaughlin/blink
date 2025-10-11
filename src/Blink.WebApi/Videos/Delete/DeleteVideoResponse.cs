namespace Blink.WebApi.Videos.Delete;

public sealed record DeleteVideoResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public required string BlobName { get; init; }
}

