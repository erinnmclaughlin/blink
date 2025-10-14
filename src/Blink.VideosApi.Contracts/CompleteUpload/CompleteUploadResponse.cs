namespace Blink.VideosApi.Contracts.CompleteUpload;

public sealed class CompleteUploadResponse
{
    public required string BlobName { get; init; }
    public required long FileSize { get; init; }
    public bool Success { get; init; }
}

