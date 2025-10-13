namespace Blink.VideosApi.Contracts.GetUrl;

public sealed record VideoUrlResponse
{
    public required string Url { get; init; }
    public string? ThumbnailUrl { get; init; }
}

