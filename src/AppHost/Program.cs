using Blink.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgresServer = builder.AddDefaultPostgresServer();
var pgDatabase = postgresServer.AddDatabase("blink-pg-db");

builder
    .AddProject<Projects.WebApp>("blink-webapp")
    .WithReference(pgDatabase)
    .WaitFor(pgDatabase);

builder.Build().Run();
