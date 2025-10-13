namespace Blink.VideosApi.Contracts.List;

public sealed record VideoSummaryDto
{
    public required string Title { get; init; }
    public required string? Description { get; init; }
    public required DateTime? VideoDate { get; init; }
    public required string? ThumbnailBlobName { get; init; }
    public required string VideoBlobName { get; init; }
}
