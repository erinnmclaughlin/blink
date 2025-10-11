using MediatR;

namespace Blink.WebApi.Videos.UpdateTitle;

public sealed record UpdateTitleCommand : IRequest<UpdateTitleResponse>
{
    public required string BlobName { get; init; }
    public required string Title { get; init; }
}

