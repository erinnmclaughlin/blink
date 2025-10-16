using Blink;
using Blink.VideosApi.Contracts;
using Blink.Web;
using Blink.Web.Client;
using Blink.Web.Components;
using Blink.Web.Videos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

// Add features.json file from Blink.Web.Client project:
builder.Configuration.AddJsonFile("features.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"features.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options =>
    {
        options.SerializeAllClaims = true;
    });

builder.AddAndConfigureKeycloak();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthorizationRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingAuthenticationStateProvider>();

builder.Services.AddFeatureManagement();
builder.Services.Configure<ConfigurationFeatureDefinitionProviderOptions>(o =>
{
    o.CustomConfigurationMergingEnabled = true;
});
builder.Services.AddScoped<IFeatureFlagManager, FeatureFlagManager>();

builder.Services.AddMediatR(o =>
{
    o.RegisterServicesFromAssemblies(typeof(Program).Assembly, VideosApiContracts.Assembly);
});

builder.AddNpgsqlDataSource(ServiceNames.BlinkDatabase);
builder.Services.AddScoped<IVideoRepository, VideoRepository>();

builder.AddBlinkStorage();

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

app.MapGet("/api/features/{featureName}", async (string featureName, IFeatureFlagManager ffManager) => await ffManager.IsEnabledAsync(featureName))
    .AllowAnonymous();

app.MapGet("/login", () => Results
    .Challenge(new AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme]))
    .AllowAnonymous();

app.MapPost("/logout", () => Results.SignOut(new AuthenticationProperties { RedirectUri = "/" }, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
    .RequireAuthorization();

app.MapVideosApi();

app.MapDefaultEndpoints();

app.Run();