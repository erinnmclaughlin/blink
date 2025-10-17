using Blink;
using Blink.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddAndConfigurePostgresServer();

var keycloak = builder.AddAndConfigureKeycloak(postgres);

var storage = builder.AddAndConfigureAzureStorage();

var messaging = builder.AddRabbitMQ(ServiceNames.Messaging);

var blinkDatabase = postgres.Server.AddDatabase(ServiceNames.BlinkDatabase);

//var blinkWebApi = builder.AddProject<Projects.Blink_WebApi>(ServiceNames.BlinkWebApi)
//    .WithExternalHttpEndpoints()
//    .WithAwaitedReference(blinkDatabase)
//    .WithAwaitedReference(keycloak)
//    .WithAwaitedReference(messaging)
//    .WithAwaitedReference(storage.Blobs);

//var blinkWebApp = builder.AddProject<Projects.Blink_WebApp>(ServiceNames.BlinkWebApp)
//    .WithExternalHttpEndpoints()
//    .WithAwaitedReference(blinkWebApi)
//    .WithAwaitedReference(keycloak);

var blinkWebApp = builder
    .AddProject<Projects.Blink_Web>(ServiceNames.BlinkWebApp)
    .WithExternalHttpEndpoints()
    .WithAwaitedReference(blinkDatabase)
    .WithAwaitedReference(keycloak)
    .WithAwaitedReference(messaging)
    .WithAwaitedReference(storage.Blobs);

//blinkWebApi
//    .WithReference(blinkWebApp);

if (OperatingSystem.IsWindows())
{
    builder
        .AddDockerfile("blink-video-processor", "../..", "src/Blink.VideoProcessor/Dockerfile")
        .WithReference(messaging)
        .WithReference(storage.Blobs);
}

builder.Build().Run();
