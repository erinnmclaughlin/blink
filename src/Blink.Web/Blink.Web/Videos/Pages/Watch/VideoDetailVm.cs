using Blink.Web.Components.Shared;

namespace Blink.Web.Videos.Pages.Watch;

public sealed record VideoDetailVm
{
    public required Guid Id { get; init; }
    public required string BlobName { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public List<MentionMetadata>? DescriptionMentions { get; init; }
    public DateOnly? VideoDate { get; init; }
    public string? ThumbnailBlobName { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
