namespace Blink.VideosApi.Contracts.CompleteUpload;

public sealed class CompleteUploadRequest : IRequest<CompleteUploadResponse>
{
    public required string BlobName { get; init; }
    public required string FileName { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? VideoDate { get; init; }
}

