using Blink.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var pgDatabase = builder
    .AddDefaultPostgresServer()
    .AddDatabase("blink-db");

var apiService = builder
    .AddProject<Projects.ApiService>("blink-api")
    .WithReference(pgDatabase)
    .WaitFor(pgDatabase);

var blinkUi = builder
    .AddProject<Projects.Web>("blink-ui")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
