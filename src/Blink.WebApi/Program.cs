using Blink;
using Blink.WebApi;
using Blink.WebApi.Videos;
using Blink.WebApi.Videos.Thumbnails;
using Blink.WebApi.Videos.Upload;
using FluentMigrator.Runner;
using FluentValidation;
using MassTransit;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.AddServiceDefaults();

builder.AddAndConfigureAuthentication();

builder.AddAndConfigureCors();

builder.AddNpgsqlDataSource(ServiceNames.BlinkDatabase);
builder.AddAndConfigureFluentMigrations();

builder.AddAndConfigureServiceBus();

builder.AddAzureBlobServiceClient(ServiceNames.AzureBlobStorage);
builder.AddAzureQueueServiceClient(ServiceNames.AzureQueueStorage);
builder.Services.AddScoped<IVideoStorageClient, VideoStorageClient>();
builder.Services.AddScoped<IVideoRepository, VideoRepository>();
builder.Services.AddScoped<IMultipartFormFileParser, MultipartFormFileParser>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ICurrentUser, CurrentUserAccessor>();

// Register thumbnail generation services
builder.Services.AddSingleton<IThumbnailQueue, ThumbnailQueue>();
builder.Services.AddScoped<IThumbnailGenerator, SimpleThumbnailGenerator>();
//builder.Services.AddHostedService<ThumbnailGenerationService>();
builder.Services.AddScoped<IVideoMetadataExtractor, FFprobeMetadataExtractor>();


builder.Services.AddMediatR(o =>
{
    o.AddOpenBehavior(typeof(ValidationBehavior<,>));
    o.RegisterServicesFromAssembly(typeof(Program).Assembly);
});

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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

// Enable global exception handling
app.UseExceptionHandler();

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

// Map all video endpoints using CQRS pattern with MediatR
app.MapVideosApi();

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    migrator.MigrateUp();
}

app.Run();
