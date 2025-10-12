using Blink;
using Blink.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddAndConfigureKeycloak();

var storage = builder.AddAndConfigureAzureStorage();

var messaging = builder
    .AddRabbitMQ(ServiceNames.Messaging)
    .WithDataVolume();

var blinkDatabase = builder
    .AddAndConfigurePostgresServer()
    .AddDatabase(ServiceNames.BlinkDatabase);

var blinkWebApi = builder.AddProject<Projects.Blink_WebApi>(ServiceNames.BlinkWebApi)
    .WithExternalHttpEndpoints()
    .WithAwaitedReference(blinkDatabase)
    .WithAwaitedReference(keycloak)
    .WithAwaitedReference(messaging)
    .WithAwaitedReference(storage.Blobs);

var blinkWebApp = builder
    .AddProject<Projects.Blink_WebApp>(ServiceNames.BlinkWebApp)
    .WithExternalHttpEndpoints()
    .WithAwaitedReference(blinkWebApi);

blinkWebApi.WithReference(blinkWebApp);

builder
    .AddProject<Projects.Blink_VideoProcessor>("blink-video-processor")
    .WithAwaitedReference(messaging)
    .WithAwaitedReference(storage.Blobs);

builder.Build().Run();
