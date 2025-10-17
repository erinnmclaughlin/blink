namespace Blink.VideoSummarizer;

/// <summary>
/// Interface for generating AI summaries of video content
/// </summary>
public interface IVideoSummarizer
{
    /// <summary>
    /// Generates an AI summary of a video
    /// </summary>
    /// <param name="videoStream">The video stream to analyze</param>
    /// <param name="videoMetadata">Metadata about the video (title, description, etc.)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A summary of the video content</returns>
    Task<string?> GenerateSummaryAsync(
        Stream videoStream, 
        VideoMetadata videoMetadata, 
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Metadata about a video for summary generation
/// </summary>
public sealed record VideoMetadata
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
}

