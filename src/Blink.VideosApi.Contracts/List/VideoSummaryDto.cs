namespace Blink.VideosApi.Contracts.List;

public sealed record VideoSummaryDto
{
    public required string Title { get; init; }
    public required string? Description { get; init; }
    public required double? DurationInSeconds { get; init; }
    public required DateOnly? VideoDate { get; init; }
    public required DateOnly UploadedAt { get; init; }
    public required long SizeInBytes { get; init; }
    public required string? ThumbnailBlobName { get; init; }
    public required string VideoBlobName { get; init; }
}
