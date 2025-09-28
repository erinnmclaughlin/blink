using Blink.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var postgresServer = builder.AddDefaultPostgresServer();
var pgDatabase = postgresServer.AddDatabase("blink-pg-db");

var papercut = builder.AddPapercutSmtp("papercut");

builder
    .AddProject<Projects.WebApp>("blink-webapp")
    .WithReference(papercut)
    .WithReference(pgDatabase)
    .WaitFor(papercut)
    .WaitFor(pgDatabase);

builder.Build().Run();
