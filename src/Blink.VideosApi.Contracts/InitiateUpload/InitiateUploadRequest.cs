namespace Blink.VideosApi.Contracts.InitiateUpload;

public sealed class InitiateUploadRequest : IRequest<InitiateUploadResponse>
{
    public required string FileName { get; init; }
}

