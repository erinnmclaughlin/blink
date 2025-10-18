using Blink.Web.Components.Shared;
using MediatR;

namespace Blink.Web.Videos.Pages.Upload;

public sealed record RegisterUploadedVideoCommand : IRequest
{
    public required string BlobName { get; init; }
    public required string FileName { get; init; }
    public required long SizeInBytes { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public DateTime? VideoDate { get; init; }
    public List<MentionMetadata>? DescriptionMentions { get; init; }

    public string GetContentType()
    {
        var extension = Path.GetExtension(FileName).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".avi" => "video/x-msvideo",
            ".wmv" => "video/x-ms-wmv",
            _ => "application/octet-stream"
        };
    }
}
