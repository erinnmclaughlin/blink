using Blink.WebApi;
using Blink.WebApi.Services;
using FluentMigrator.Runner;
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

builder.AddBlobStorage();

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

app.MapPost("/api/videos/upload", async (HttpContext context, IBlobStorageService blobStorageService, ILogger<Program> logger) =>
{
    try
    {
        var form = await context.Request.ReadFormAsync();
        var file = form.Files.GetFile("video");

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "No video file provided" });
        }

        // Validate file size (e.g., max 500MB)
        const long maxFileSize = 500L * 1024 * 1024;
        if (file.Length > maxFileSize)
        {
            return Results.BadRequest(new { error = "File size exceeds maximum allowed size of 500MB" });
        }

        // Validate file type
        var allowedExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return Results.BadRequest(new { error = $"Invalid file type. Allowed types: {string.Join(", ", allowedExtensions)}" });
        }

        using var stream = file.OpenReadStream();
        var blobName = await blobStorageService.UploadVideoAsync(stream, file.FileName);

        logger.LogInformation("Video uploaded successfully: {FileName} -> {BlobName}", file.FileName, blobName);

        return Results.Ok(new { 
            message = "Video uploaded successfully",
            blobName = blobName,
            fileName = file.FileName,
            fileSize = file.Length
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error uploading video");
        return Results.Problem("An error occurred while uploading the video");
    }
})
    .WithName("UploadVideo")
    .RequireAuthorization()
    .DisableAntiforgery();

app.MapGet("/api/videos", async (IBlobStorageService blobStorageService, ILogger<Program> logger) =>
{
    try
    {
        var videos = await blobStorageService.ListVideosAsync();
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

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    migrator.MigrateUp();
}

app.Run();
