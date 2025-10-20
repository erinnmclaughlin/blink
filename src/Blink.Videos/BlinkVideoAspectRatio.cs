namespace Blink.Videos;

public sealed record BlinkVideoAspectRatio
{
    /// <summary>
    /// The width of the video.
    /// </summary>
    public required int Width { get; init; }
    
    /// <summary>
    /// The height of the video.
    /// </summary>
    public required int Height { get; init; }

    private BlinkVideoAspectRatio() { }

    public static BlinkVideoAspectRatio Create(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(width, 0, nameof(width));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(height, 0, nameof(height));

        return new BlinkVideoAspectRatio
        {
            Width = width,
            Height = height
        };
    }
}
