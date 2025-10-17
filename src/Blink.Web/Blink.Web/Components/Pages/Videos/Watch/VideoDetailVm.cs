namespace Blink.Web.Components.Pages.Videos.Watch;

public sealed record VideoDetailVm
{
    public required Guid Id { get; init; }
    public required string BlobName { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime? VideoDate { get; init; }
    public string? ThumbnailBlobName { get; init; }
    public DateTime UploadedAt { get; init; }
}
