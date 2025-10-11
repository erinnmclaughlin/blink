using Blink.WebApi;
using FluentMigrator.Runner;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

builder.AddServiceDefaults();

builder.AddNpgsqlDataSource("blinkdb");
builder.AddFluentMigrations();

builder.AddKnownClientsCorsPolicy();

builder.AddKeycloakAuthorization();
//builder.AddKeycloakEventPoller();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors(CorsConfiguration.KnownClientPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/test", () => "Hello, Blink!")
    .WithName("TestEndpoint");

app.MapGet("/test-auth", () => "Hello, Authorized Blink!")
    .WithName("TestAuthEndpoint")
    .RequireAuthorization();

app.MapGet("/claims", (ClaimsPrincipal user) => Results.Json(user.Claims.Select(c => new { c.Type, c.Value }).GroupBy(c => c.Type, c => c.Value).ToDictionary(g => g.Key, g => g.ToArray())))
    .WithName("ClaimsEndpoint")
    .RequireAuthorization();

app.MapDefaultEndpoints();

using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    migrator.MigrateUp();
}

app.Run();
