using Blink.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAndConfigureAzureStorage();
var serviceBus = builder.AddAndConfigureServiceBus();

var keycloak = builder.AddAndConfigureKeycloak();

var blinkDb = builder.AddAndConfigurePostgresServer().AddDatabase("blinkdb");

var blinkApi = builder.AddProject<Projects.Blink_WebApi>("blinkapi")
    .WithExternalHttpEndpoints()
    .WithAwaitedReference(blinkDb)
    .WithAwaitedReference(keycloak)
    .WithAwaitedReference(storage.Blobs)
    .WithAwaitedReference(storage.Queues)
    .WithAwaitedReference(serviceBus);

var blinkWebApp = builder
    .AddProject<Projects.Blink_WebApp>("blink-webapp")
    .WithExternalHttpEndpoints()
    .WithAwaitedReference(blinkApi);

blinkApi.WithReference(blinkWebApp);

builder.Build().Run();
