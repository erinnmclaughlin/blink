namespace Blink.VideosApi.Contracts.InitiateUpload;

public sealed class InitiateUploadResponse
{
    public required string BlobName { get; init; }
    public required string UploadUrl { get; init; }
}

