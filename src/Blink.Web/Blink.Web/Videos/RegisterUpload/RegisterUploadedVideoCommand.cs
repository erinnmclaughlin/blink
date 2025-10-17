using MediatR;

namespace Blink.Web.Videos.RegisterUpload;

public sealed record RegisterUploadedVideoCommand : IRequest
{
    public required string BlobName { get; init; }
    public required string FileName { get; init; }
    public required long SizeInBytes { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? VideoDate { get; init; }
}

