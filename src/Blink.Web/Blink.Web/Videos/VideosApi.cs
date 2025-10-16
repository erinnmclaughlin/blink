using Blink.Storage;
using Blink.VideosApi.Contracts.GetRecentUploads;
using Blink.VideosApi.Contracts.GetUrl;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blink.Web.Videos;

/// <summary>
/// Videos API endpoints following CQRS pattern with MediatR
/// </summary>
public static class VideosApi
{
    public static IEndpointRouteBuilder MapVideosApi(this IEndpointRouteBuilder endpoints)
    {
        // GET /api/videos/{blobName}/url
        endpoints.MapGet("/api/videos/{blobName}/url", HandleGetVideoUrlAsync)
            .WithName("GetVideoUrl")
            .RequireAuthorization()
            .Produces<VideoUrlResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        
        return endpoints;
    }

    private static async Task<IResult> HandleGetVideoUrlAsync(
        string blobName,
        ISender sender,
        CancellationToken cancellationToken)
        => Results.Ok(await sender.Send(new GetVideoUrlQuery { BlobName = blobName }, cancellationToken));
}
