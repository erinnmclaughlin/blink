using Blink.WebApi.Keycloak;
using FluentMigrator.Runner;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.AddServiceDefaults();
builder.AddNpgsqlDataSource("blinkdb");

builder.Services.AddAuthorization();

builder.Services.AddAuthentication()
    .AddKeycloakJwtBearer("keycloak", "blink", o =>
    {
        o.Audience = "account";
        o.RequireHttpsMetadata = builder.Environment.IsProduction();
    });

builder.Services
    .AddFluentMigratorCore()
    .ConfigureRunner(rb =>
    {
        rb.AddPostgres();
        rb.WithGlobalConnectionString(builder.Configuration.GetConnectionString("blinkdb"));
        rb.ScanIn(typeof(Program).Assembly).For.All();
    })
    .AddLogging(lb => lb.AddFluentMigratorConsole());

builder.Services
    .Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"))
    .AddHttpClient("keycloak", (sp, client) =>
    {
        var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KeycloakOptions>>().Value;
        client.BaseAddress = new Uri(opt.BaseUrl);
    });

builder.Services.AddHostedService<KeycloakEventPoller>();
builder.Services.Configure<HostOptions>(o =>
{
    o.ServicesStartConcurrently = true;
    o.ServicesStopConcurrently = true;
});

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/test", () => "Hello, Blink!")
    .WithName("TestEndpoint");

app.MapGet("/test-auth", () => "Hello, Authorized Blink!")
    .WithName("TestAuthEndpoint")
    .RequireAuthorization();

app.MapGet("/claims", (ClaimsPrincipal user) => Results.Json(user.Claims.Select(c => new { c.Type, c.Value })))
    .WithName("ClaimsEndpoint")
    .RequireAuthorization();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    migrator.MigrateUp();
}

app.Run();
