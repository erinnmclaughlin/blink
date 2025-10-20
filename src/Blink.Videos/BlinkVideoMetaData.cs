namespace Blink.Videos;

public sealed record BlinkVideoMetaData
{
    /// <summary>
    /// The video's aspect ratio.
    /// </summary>
    public BlinkVideoAspectRatio? AspectRatio { get; init; }
    
    /// <summary>
    /// The duration of the video.
    /// </summary>
    public TimeSpan? Duration { get; init; }
}
