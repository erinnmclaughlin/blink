using Blink.Web.Components;
using Blink.Web.Configuration;
using Blink.Web.Features.People;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

// Add features.json file from Blink.Web.Client project:
builder.Configuration.AddJsonFile("features.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"features.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(o => o.SerializeAllClaims = true);

builder.AddAndConfigureAuth();
builder.AddAndConfigureDatabase();
builder.AddAndConfigureServiceBus();
builder.AddBlinkStorage();

builder.Services.AddFeatureManagement();
builder.Services.Configure<ConfigurationFeatureDefinitionProviderOptions>(o => o.CustomConfigurationMergingEnabled = true);

builder.Services.AddMediatR(o => o.RegisterServicesFromAssemblies(typeof(Program).Assembly));

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
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Blink.Web.Client._Imports).Assembly);

app.MapGet("/login", () => Results
    .Challenge(new AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme]))
    .AllowAnonymous();

app.MapPost("/logout", () => Results.SignOut(new AuthenticationProperties { RedirectUri = "/" }, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
    .RequireAuthorization();

app.MapPeopleEndpoints();

app.MapDefaultEndpoints();

app.Run();