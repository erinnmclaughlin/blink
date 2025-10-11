using Blink.WebApi;
using Blink.WebApi.Videos;
using Blink.WebApi.Videos.Upload;
using FluentMigrator.Runner;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
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

// Configure Kestrel to allow larger request bodies (500MB for video uploads)
builder.Services.Configure<KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024; // 500MB
});

// Configure Form options to allow larger multipart form data
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024; // 500MB
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

app.MapPost("/api/videos/upload", async (HttpContext context, ISender sender) =>
{
    var form = await context.Request.ReadFormAsync();

    var file = form.Files.GetFile("video");

    if (file is null)
    {
        return Results.BadRequest(new { error = "No video file provided" });
    }

    var request = new UploadVideoRequest { File = file };
    var uploadedVideo = await sender.Send(request, context.RequestAborted);
    return Results.Ok(uploadedVideo);
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
