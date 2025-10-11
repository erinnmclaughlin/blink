using Blink.WebApi.Videos.Delete;
using Blink.WebApi.Videos.GetUrl;
using Blink.WebApi.Videos.List;
using Blink.WebApi.Videos.Upload;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blink.WebApi.Videos;

/// <summary>
/// Videos API endpoints following CQRS pattern with MediatR
/// </summary>
public static class VideosApi
{
    public static IEndpointRouteBuilder MapVideosApi(this IEndpointRouteBuilder endpoints)
    {
        // POST /api/videos/upload
        endpoints.MapPost("/api/videos/upload", HandleUploadAsync)
            .WithName("UploadVideo")
            .RequireAuthorization()
            .DisableAntiforgery()
            .Produces<UploadedVideoInfo>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

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

        // DELETE /api/videos/{blobName}
        endpoints.MapDelete("/api/videos/{blobName}", HandleDeleteVideoAsync)
            .WithName("DeleteVideo")
            .RequireAuthorization()
            .Produces<DeleteVideoResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<IResult> HandleUploadAsync(
        HttpContext context,
        IMultipartFormFileParser fileParser,
        ISender sender,
        ILogger<Program> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Video upload request received. ContentType: {ContentType}, ContentLength: {ContentLength}",
            context.Request.ContentType,
            context.Request.ContentLength);

        // Parse the multipart form file
        var file = await fileParser.ParseFileAsync(context, cancellationToken);
        
        if (file == null)
        {
            logger.LogWarning("No video file found in request");
            throw new ArgumentException("No video file provided or invalid content type");
        }

        logger.LogInformation("Processing upload for file: {FileName}", file.FileName);

        // Send command through MediatR pipeline (validation happens automatically via ValidationBehavior)
        var request = new UploadVideoRequest { File = file };
        var result = await sender.Send(request, cancellationToken);

        logger.LogInformation("Video uploaded successfully: {BlobName}", result.BlobName);
        return Results.Ok(result);
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

    private static async Task<IResult> HandleDeleteVideoAsync(
        string blobName,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Send command through MediatR pipeline
        var command = new DeleteVideoCommand { BlobName = blobName };
        var result = await sender.Send(command, cancellationToken);
        
        return Results.Ok(result);
    }
}
