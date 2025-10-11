using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Blink.WebApi.Videos.Upload;

/// <summary>
/// Endpoint for uploading videos following CQRS pattern
/// </summary>
public static class UploadVideoEndpoint
{
    public static IEndpointRouteBuilder MapUploadVideoEndpoint(this IEndpointRouteBuilder endpoints)
    {
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
        try
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
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid Request",
                    Detail = "No video file provided or invalid content type",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            logger.LogInformation("Processing upload for file: {FileName}", file.FileName);

            // Send command through MediatR pipeline
            var request = new UploadVideoRequest { File = file };
            var result = await sender.Send(request, cancellationToken);

            logger.LogInformation("Video uploaded successfully: {BlobName}", result.BlobName);
            return Results.Ok(result);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed for video upload");
            return Results.ValidationProblem(
                ex.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()),
                title: "Validation Failed",
                statusCode: StatusCodes.Status400BadRequest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing video upload");
            return Results.Problem(
                detail: ex.Message,
                title: "Upload Failed",
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}

