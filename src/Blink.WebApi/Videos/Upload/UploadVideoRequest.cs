using MediatR;

namespace Blink.WebApi.Videos.Upload;

public sealed record UploadVideoRequest : IRequest<UploadedVideoInfo>
{
    public required IFormFile File { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? VideoDate { get; init; }

    public string FileExtension => Path.GetExtension(File.FileName).ToLowerInvariant();
}
