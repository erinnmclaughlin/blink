namespace Blink.WebApi.Videos.UpdateTitle;

public sealed record UpdateTitleResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public required string BlobName { get; init; }
    public required string Title { get; init; }
}

