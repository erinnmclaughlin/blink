using Blink.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgresServer = builder.AddDefaultPostgresServer();

var pgDatabase = postgresServer.AddDatabase("blink-db");
var pgDatabase2 = postgresServer.AddDatabase("blink-db2");

var apiService = builder
    .AddProject<Projects.ApiService>("blink-api")
    .WithReference(pgDatabase)
    .WaitFor(pgDatabase);

var blinkUi = builder
    .AddProject<Projects.Web>("blink-ui")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.WebApp>("webapp").WithReference(pgDatabase2).WaitFor(pgDatabase2);

builder.Build().Run();
