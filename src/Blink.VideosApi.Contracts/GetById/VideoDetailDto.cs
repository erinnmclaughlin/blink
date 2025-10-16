namespace Blink.VideosApi.Contracts.GetById;

public sealed record VideoDetailDto
{
    public required Guid Id { get; init; }
    public required string BlobName { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime? VideoDate { get; init; }
    public string? ThumbnailBlobName { get; init; }
    public DateTime UploadedAt { get; init; }
}
