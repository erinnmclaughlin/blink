using Blink.WebApi.Videos.Upload;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blink.WebApi.Videos;

/// <summary>
/// Endpoint for uploading videos following CQRS pattern
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
}
