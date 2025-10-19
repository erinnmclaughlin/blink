using Blink.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddAndConfigurePostgresServer();

var keycloak = builder.AddAndConfigureKeycloak(postgres);

var storage = builder.AddAndConfigureAzureStorage();

var messaging = builder.AddRabbitMQ("blink-messaging").WithLifetime(ContainerLifetime.Persistent);

var blinkDatabase = postgres.Server.AddDatabase("blink-db");

var databaseMigrator = builder
    .AddProject<Projects.Blink_DatabaseMigrator>("blink-db-migrator")
    .WithAwaitedReference(blinkDatabase);

var blinkWebApp = builder
    .AddProject<Projects.Blink_Web>("blink-webapp")
    .WithExternalHttpEndpoints()
    .WithAwaitedReference(blinkDatabase)
    .WithAwaitedReference(keycloak)
    .WithAwaitedReference(messaging)
    .WithAwaitedReference(storage.Blobs);

builder
    .AddProject<Projects.Blink_ThumbnailGenerator>("blink-thumbnail-generator")
    .WithAwaitedReference(messaging)
    .WithAwaitedReference(storage.Blobs);

if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
{
    builder
        .AddProject<Projects.Blink_VideoProcessor>("blink-video-processor")
        //.AddDockerfile("blink-video-processor", "../..", "src/Blink.VideoProcessor/Dockerfile")
        .WithReference(messaging)
        .WithReference(storage.Blobs)
        .WaitFor(messaging)
        .WaitFor(storage.Blobs);
}

builder.Build().Run();
