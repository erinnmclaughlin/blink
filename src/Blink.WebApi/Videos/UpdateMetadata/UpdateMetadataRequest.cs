namespace Blink.WebApi.Videos.UpdateMetadata;

public sealed record UpdateMetadataRequest(
    string Title,
    string? Description,
    DateTime? VideoDate
);

