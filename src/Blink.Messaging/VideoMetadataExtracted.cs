using Blink.Videos;

namespace Blink.Messaging;

/// <summary>
/// Video metadata extracted from the video file
/// </summary>
public sealed record VideoMetadataExtracted
{
    public required Guid VideoId { get; init; }
    public required BlinkVideoMetaData Metadata { get; init; }
}
