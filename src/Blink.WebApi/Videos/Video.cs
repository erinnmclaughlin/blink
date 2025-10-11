namespace Blink.WebApi.Videos;

/// <summary>
/// Represents a video entity in the database
/// </summary>
public sealed class Video
{
    public Guid Id { get; set; }
    public required string BlobName { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public DateTime? VideoDate { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public long SizeInBytes { get; set; }
    public required string OwnerId { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

