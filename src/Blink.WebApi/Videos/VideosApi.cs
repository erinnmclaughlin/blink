using Blink.Storage;
using Blink.VideosApi.Contracts.Delete;
using Blink.VideosApi.Contracts.GetByBlobName;
using Blink.VideosApi.Contracts.GetUrl;
using Blink.VideosApi.Contracts.List;
using Blink.VideosApi.Contracts.UpdateMetadata;
using Blink.VideosApi.Contracts.UpdateTitle;
using Blink.VideosApi.Contracts.Upload;
using Blink.WebApi.Videos.UpdateMetadata;
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
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError)
            .WithRequestTimeout(TimeSpan.FromMinutes(30)); // 30 minute timeout for large uploads

        // GET /api/videos
        endpoints.MapGet("/api/videos", HandleListVideosAsync)
            .WithName("ListVideos")
            .RequireAuthorization()
            .Produces<List<VideoInfo>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // GET /api/videos/{blobName}
        endpoints.MapGet("/api/videos/{blobName}", HandleGetVideoByBlobNameAsync)
            .WithName("GetVideoByBlobName")
            .RequireAuthorization()
            .Produces<VideoDetailDto>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
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

        // PUT /api/videos/{blobName}/title
        endpoints.MapPut("/api/videos/{blobName}/title", HandleUpdateTitleAsync)
            .WithName("UpdateVideoTitle")
            .RequireAuthorization()
            .Produces<UpdateTitleResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

        // PUT /api/videos/{blobName}/metadata
        endpoints.MapPut("/api/videos/{blobName}/metadata", HandleUpdateMetadataAsync)
            .WithName("UpdateVideoMetadata")
            .RequireAuthorization()
            .Produces<UpdateMetadataResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
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

        // Parse the multipart form data (file and fields)
        var formData = await fileParser.ParseAsync(context, cancellationToken);
        
        if (formData.File == null)
        {
            logger.LogWarning("No video file found in request");
            throw new ArgumentException("No video file provided or invalid content type");
        }

        logger.LogInformation("Processing upload for file: {FileName}", formData.File.FileName);

        // Extract additional form fields
        formData.Fields.TryGetValue("title", out var title);
        formData.Fields.TryGetValue("description", out var description);
        DateTime? videoDate = null;
        if (formData.Fields.TryGetValue("videoDate", out var videoDateStr) && 
            DateTime.TryParse(videoDateStr, out var parsedDate))
        {
            videoDate = parsedDate;
        }

        // Send command through MediatR pipeline (validation happens automatically via ValidationBehavior)
        var request = new UploadVideoRequest 
        { 
            File = formData.File,
            Title = string.IsNullOrWhiteSpace(title) ? null : title,
            Description = string.IsNullOrWhiteSpace(description) ? null : description,
            VideoDate = videoDate
        };
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

    private static async Task<IResult> HandleGetVideoByBlobNameAsync(
        string blobName,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetVideoByBlobNameQuery(blobName);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
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

    private static async Task<IResult> HandleUpdateTitleAsync(
        string blobName,
        [FromBody] UpdateTitleRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Send command through MediatR pipeline
        var command = new UpdateTitleCommand { BlobName = blobName, Title = request.Title };
        var result = await sender.Send(command, cancellationToken);
        
        return Results.Ok(result);
    }

    private static async Task<IResult> HandleUpdateMetadataAsync(
        string blobName,
        [FromBody] UpdateMetadataRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        // Send command through MediatR pipeline
        var command = new UpdateMetadataCommand 
        { 
            BlobName = blobName, 
            Title = request.Title,
            Description = request.Description,
            VideoDate = request.VideoDate
        };
        var result = await sender.Send(command, cancellationToken);
        
        return Results.Ok(result);
    }
}

public sealed record UpdateTitleRequest(string Title);
