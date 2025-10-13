using Blink;
using Blink.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddAndConfigurePostgresServer();

var keycloak = builder.AddAndConfigureKeycloak(postgres);

var storage = builder.AddAndConfigureAzureStorage();

var messaging = builder.AddRabbitMQ(ServiceNames.Messaging);

var blinkDatabase = postgres.Server.AddDatabase(ServiceNames.BlinkDatabase);

var blinkWebApi = builder.AddProject<Projects.Blink_WebApi>(ServiceNames.BlinkWebApi)
    .WithExternalHttpEndpoints()
    .WithAwaitedReference(blinkDatabase)
    .WithAwaitedReference(keycloak)
    .WithAwaitedReference(messaging)
    .WithAwaitedReference(storage.Blobs);

var blinkWebApp = builder
    .AddProject<Projects.Blink_BlazorSSR>(ServiceNames.BlinkWebApp)
    .WithExternalHttpEndpoints()
    .WithAwaitedReference(blinkWebApi)
    .WithAwaitedReference(keycloak);

blinkWebApi
    .WithReference(blinkWebApp);

builder
    .AddProject<Projects.Blink_VideoProcessor>("blink-video-processor")
    .WithAwaitedReference(messaging)
    .WithAwaitedReference(storage.Blobs);

builder.Build().Run();
