namespace Blink.VideosApi.Contracts.UpdateTitle;

public sealed record UpdateTitleCommand : IRequest<UpdateTitleResponse>
{
    public required string BlobName { get; init; }
    public required string Title { get; init; }
}

