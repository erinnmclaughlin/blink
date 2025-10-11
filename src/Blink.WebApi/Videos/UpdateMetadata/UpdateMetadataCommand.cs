using MediatR;

namespace Blink.WebApi.Videos.UpdateMetadata;

public sealed record UpdateMetadataCommand : IRequest<UpdateMetadataResponse>
{
    public required string BlobName { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public DateTime? VideoDate { get; init; }
}

