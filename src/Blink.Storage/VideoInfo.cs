namespace Blink.Storage;

public sealed record VideoInfo
{
    public required string BlobName { get; init; }
    public required string FileName { get; init; }
    public required long SizeInBytes { get; init; }
    public required DateTimeOffset? LastModified { get; init; }
    public required string ContentType { get; init; }
}
