namespace Blink.Messaging;

public sealed record VideoThumbnailGenerated
{
    public required string VideoBlobName { get; init; }
    public required string ThumbnailBlobName { get; init; }
}
