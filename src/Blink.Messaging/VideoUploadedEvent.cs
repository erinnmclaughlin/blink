namespace Blink.Messaging;

/// <summary>
/// Event published when a video has been successfully uploaded
/// </summary>
public sealed record VideoUploadedEvent
{
    public required Guid VideoId { get; init; }
    public required string BlobName { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string OwnerId { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public long SizeInBytes { get; init; }
    public DateTime UploadedAt { get; init; }
}
