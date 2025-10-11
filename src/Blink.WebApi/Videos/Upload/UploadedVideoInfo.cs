namespace Blink.WebApi.Videos.Upload;

public sealed record UploadedVideoInfo
{
    public required string BlobName { get; init; }
    public required string FileName { get; init; }
    public required long FileSize { get; init; }
}