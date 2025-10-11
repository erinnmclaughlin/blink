namespace Blink.WebApi.Videos.UpdateMetadata;

public sealed record UpdateMetadataResponse
{
    public required bool Success { get; init; }
    public required string Message { get; init; }
    public required string BlobName { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime? VideoDate { get; init; }
}

