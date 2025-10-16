namespace Blink.VideosApi.Contracts;

public sealed record VideoSummaryDto
{
    public required Guid Id { get; init; }
    public required string BlobName { get; init; }
    public required string Title { get; init; }
    public required double? DurationInSeconds { get; init; }
    public required long SizeInBytes { get; init; }
    public required DateTimeOffset UploadedAt { get; init; }
    public required DateOnly? VideoDate { get; init; }
}