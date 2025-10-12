using Blink;
using Blink.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder.AddAndConfigureKeycloak();

var serviceBus = builder.AddAndConfigureServiceBus();
var storage = builder.AddAndConfigureAzureStorage();

var blinkDatabase = builder
    .AddAndConfigurePostgresServer()
    .AddDatabase(ServiceNames.BlinkDatabase);

var blinkWebApi = builder.AddProject<Projects.Blink_WebApi>(ServiceNames.BlinkWebApi)
    .WithExternalHttpEndpoints()
    .WithAwaitedReference(blinkDatabase)
    .WithAwaitedReference(keycloak)
    .WithAwaitedReference(storage.Blobs)
    .WithAwaitedReference(storage.Queues);

var blinkWebApp = builder
    .AddProject<Projects.Blink_WebApp>(ServiceNames.BlinkWebApp)
    .WithExternalHttpEndpoints()
    .WithAwaitedReference(blinkWebApi);

blinkWebApi.WithReference(blinkWebApp);

builder.Build().Run();
