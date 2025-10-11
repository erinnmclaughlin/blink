var builder = DistributedApplication.CreateBuilder(args);

var keycloak = builder
    .AddKeycloak("keycloak", 8080)
    .WithExternalHttpEndpoints()
    .WithDataVolume();

var pgUsername = builder.AddParameter("pg-username", secret: true);
var pgPassword = builder.AddParameter("pg-password", secret: true);

var pgServer = builder
    .AddPostgres("pg-server", pgUsername, pgPassword)
    .WithDataVolume()
    .WithPgWeb();

var papercut = builder.AddPapercutSmtp("papercut");
var blinkDb = pgServer.AddDatabase("blinkdb");

var keycloakAdminClientId = builder.AddParameter("keycloak-clientid", "user-sync-job-test");
var keycloakAdminClientSecret = builder.AddParameter("keycloak-clientsecret", secret: true);

var blinkApi = builder.AddProject<Projects.Blink_WebApi>("blinkapi")
    .WithExternalHttpEndpoints()
    .WithReference(blinkDb)
    .WithReference(papercut)
    .WithReference(keycloak)
    .WithEnvironment("Keycloak:ClientId", keycloakAdminClientId)
    .WithEnvironment("Keycloak:Clientsecret", keycloakAdminClientSecret)
    .WaitFor(blinkDb)
    .WaitFor(keycloak);

builder
    .AddProject<Projects.Blink_WebApp>("blink-webapp")
    .WithExternalHttpEndpoints()
    .WithReference(blinkApi);


builder.Build().Run();
