namespace Blink.WebApi.Videos;

public sealed record VideoDetailDto
{
    public required string Title { get; init; }
    public required string? Description { get; init; }
    public required DateTime? VideoDate { get; init; }
    public required string? ThumbnailBlobName { get; init; }
    public required string VideoBlobName { get; init; }
    public required string VideoWatchUrl { get; init; }
}

public sealed record VideoSummaryDto
{
    public required string Title { get; init; }
    public required string? Description { get; init; }
    public required DateTime? VideoDate { get; init; }
    public required string? ThumbnailBlobName { get; init; }
    public required string VideoBlobName { get; init; }
}