namespace Blink.Messaging;

public sealed record VideoThumbnailGenerated
{
    public required Guid VideoId { get; init; }
    public required string ThumbnailBlobName { get; init; }
}