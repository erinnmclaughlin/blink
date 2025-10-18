namespace Blink.Messaging;

/// <summary>
/// Video metadata extracted from the video file
/// </summary>
public sealed record VideoMetadataExtracted
{
    public required string VideoBlobName { get; init; }
    public required VideoMetadata Metadata { get; init; }
}

public sealed record VideoMetadata
{
    public int Width { get; init; }
    public int Height { get; init; }
    public double DurationInSeconds { get; init; }
}
