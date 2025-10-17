using Blink;
using Blink.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddAndConfigurePostgresServer();

var keycloak = builder.AddAndConfigureKeycloak(postgres);

var storage = builder.AddAndConfigureAzureStorage();

var messaging = builder.AddRabbitMQ(ServiceNames.Messaging);

var blinkDatabase = postgres.Server.AddDatabase(ServiceNames.BlinkDatabase);

var databaseMigrator = builder
    .AddProject<Projects.Blink_DatabaseMigrator>(ServiceNames.BlinkDatabaseMigrator)
    .WithAwaitedReference(blinkDatabase);

var blinkWebApp = builder
    .AddProject<Projects.Blink_Web>(ServiceNames.BlinkWebApp)
    .WithExternalHttpEndpoints()
    .WithAwaitedReference(blinkDatabase)
    .WithAwaitedReference(keycloak)
    .WithAwaitedReference(messaging)
    .WithAwaitedReference(storage.Blobs);

if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
{
    builder
        .AddDockerfile(ServiceNames.BlinkVideoProcessor, "../..", "src/Blink.VideoProcessor/Dockerfile")
        .WithReference(messaging)
        .WithReference(storage.Blobs)
        .WaitFor(messaging)
        .WaitFor(storage.Blobs);
}

builder.Build().Run();
