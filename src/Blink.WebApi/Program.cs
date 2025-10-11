using Blink.WebApi;
using Blink.WebApi.Videos;
using Blink.WebApi.Videos.Upload;
using FluentMigrator.Runner;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.AddServiceDefaults();

builder.AddNpgsqlDataSource("blinkdb");
builder.AddFluentMigrations();

builder.AddKnownClientsCorsPolicy();

builder.AddKeycloakAuthorization();
//builder.AddKeycloakEventPoller();

builder.AddAzureBlobServiceClient("blobs");
builder.Services.AddScoped<IVideoStorageClient, VideoStorageClient>();

builder.Services.AddMediatR(o =>
{
    o.AddOpenBehavior(typeof(ValidationBehavior<,>));
    o.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Configure Kestrel to allow larger request bodies (2000MB for video uploads)
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 2000 * 1024 * 1024; // 2000MB
});

// Configure Form options to allow larger multipart form data
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 2000 * 1024 * 1024; // 2000MB
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors(CorsConfiguration.KnownClientPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/test", () => "Hello, Blink!")
    .WithName("TestEndpoint");

app.MapGet("/test-auth", () => "Hello, Authorized Blink!")
    .WithName("TestAuthEndpoint")
    .RequireAuthorization();

app.MapGet("/claims", (ClaimsPrincipal user) => Results.Json(user.Claims.Select(c => new { c.Type, c.Value }).GroupBy(c => c.Type, c => c.Value).ToDictionary(g => g.Key, g => g.ToArray())))
    .WithName("ClaimsEndpoint")
    .RequireAuthorization();

app.MapPost("/api/videos/upload", async (HttpContext context, ISender sender, ILogger<Program> logger) =>
{
    try
    {
        logger.LogInformation("Video upload request received. ContentType: {ContentType}, ContentLength: {ContentLength}", 
            context.Request.ContentType, context.Request.ContentLength);

        if (!context.Request.HasFormContentType || 
            !MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var mediaTypeHeader) ||
            string.IsNullOrEmpty(mediaTypeHeader.Boundary.Value))
        {
            logger.LogWarning("Invalid content type: {ContentType}", context.Request.ContentType);
            return Results.BadRequest(new { error = "Invalid content type" });
        }

        var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary.Value).Value;
        if (string.IsNullOrWhiteSpace(boundary))
        {
            logger.LogWarning("Missing boundary in content type");
            return Results.BadRequest(new { error = "Missing boundary" });
        }

        logger.LogInformation("Reading multipart data with boundary: {Boundary}", boundary);

        var reader = new MultipartReader(boundary, context.Request.Body);
        MultipartSection? section;
        StreamingFormFile? videoFile = null;

        // Read sections one at a time (streaming)
        while ((section = await reader.ReadNextSectionAsync(context.RequestAborted)) != null)
        {
            var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
                section.ContentDisposition, out var contentDisposition);

            if (hasContentDispositionHeader && contentDisposition!.DispositionType.Equals("form-data") &&
                !string.IsNullOrEmpty(contentDisposition.FileName.Value))
            {
                // This is a file
                var fileName = contentDisposition.FileName.Value;
                logger.LogInformation("Found file in multipart: {FileName}", fileName);
                videoFile = new StreamingFormFile(section.Body, fileName, section.ContentType);
                break; // We only expect one file
            }
        }

        if (videoFile == null)
        {
            logger.LogWarning("No video file found in request");
            return Results.BadRequest(new { error = "No video file provided" });
        }

        logger.LogInformation("Processing upload for file: {FileName}", videoFile.FileName);
        var request = new UploadVideoRequest { File = videoFile };
        var uploadedVideo = await sender.Send(request, context.RequestAborted);
        logger.LogInformation("Video uploaded successfully: {BlobName}", uploadedVideo.BlobName);
        return Results.Ok(uploadedVideo);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing video upload");
        return Results.Problem(detail: ex.Message, title: "Upload failed", statusCode: 500);
    }
})
    .WithName("UploadVideo")
    .RequireAuthorization()
    .DisableAntiforgery();

app.MapGet("/api/videos", async (IVideoStorageClient blobStorageService, ILogger<Program> logger) =>
{
    try
    {
        var videos = await blobStorageService.ListAsync();
        logger.LogInformation("Retrieved {Count} videos", videos.Count);
        return Results.Ok(videos);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error retrieving videos");
        return Results.Problem("An error occurred while retrieving videos");
    }
})
    .WithName("ListVideos")
    .RequireAuthorization();

app.MapGet("/api/videos/{blobName}/url", async (string blobName, IVideoStorageClient blobStorageService, ILogger<Program> logger) =>
{
    try
    {
        var url = await blobStorageService.GetUrlAsync(blobName);
        logger.LogInformation("Generated URL for video: {BlobName}", blobName);
        return Results.Ok(new { url });
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound(new { error = "Video not found" });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error generating video URL");
        return Results.Problem("An error occurred while generating video URL");
    }
})
    .WithName("GetVideoUrl")
    .RequireAuthorization();

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    migrator.MigrateUp();
}

app.Run();
