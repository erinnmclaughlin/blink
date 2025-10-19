using Blink.Web;
using Blink.Web.Components;
using Blink.Web.Configuration;
using Blink.Web.Mentions.Features;
using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("features.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"features.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddAuthenticationStateSerialization(o => o.SerializeAllClaims = true);

builder.AddBlinkAuthorization();
builder.AddBlinkDatabase();
builder.AddBlinkMessaging(o => o.AddConsumersFromNamespaceContaining<BlinkWebApp>());
builder.AddBlinkStorage();

builder.Services.AddFeatureManagement();
builder.Services.Configure<ConfigurationFeatureDefinitionProviderOptions>(o => o.CustomConfigurationMergingEnabled = true);
builder.Services.AddMediatR(o => o.RegisterServicesFromAssemblies(BlinkWebApp.Assembly));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.UseAuthentication();

app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/login", () => Results
    .Challenge(new AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme]))
    .AllowAnonymous();

app.MapPost("/logout", () => Results.SignOut(new AuthenticationProperties { RedirectUri = "/" }, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
    .RequireAuthorization();

app.MapPeopleEndpoints();

app.MapDefaultEndpoints();

app.Run();
