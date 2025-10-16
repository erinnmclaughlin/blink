using Blink.Storage;
using Blink.VideosApi.Contracts.GetUrl;
using Blink.VideosApi.Contracts.List;
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
        // GET /api/videos
        endpoints.MapGet("/api/videos", HandleListVideosAsync)
            .WithName("ListVideos")
            .RequireAuthorization()
            .Produces<List<VideoInfo>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // GET /api/videos/{blobName}/url
        endpoints.MapGet("/api/videos/{blobName}/url", HandleGetVideoUrlAsync)
            .WithName("GetVideoUrl")
            .RequireAuthorization()
            .Produces<VideoUrlResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);
        
        return endpoints;
    }
    
    private static async Task<IResult> HandleListVideosAsync(
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Send query through MediatR pipeline
        var query = new ListVideosQuery();
        var videos = await sender.Send(query, cancellationToken);
        
        return Results.Ok(videos);
    }
    
    private static async Task<IResult> HandleGetVideoUrlAsync(
        string blobName,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Send query through MediatR pipeline
        var query = new GetVideoUrlQuery { BlobName = blobName };
        var result = await sender.Send(query, cancellationToken);
        
        return Results.Ok(result);
    }
}
