using Blink.Web;
using Blink.Web.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents()
    .AddAuthenticationStateSerialization(options =>
    {
        // Preserve the full claim set when switching to WASM
        options.SerializeAllClaims = true;
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<AuthorizationRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingAuthenticationStateProvider>();

// Configure Keycloak Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddKeycloakOpenIdConnect(
        serviceName: "keycloak",
        realm: "blink",
        options =>
        {
            options.ClientId = "blink-webapp";
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.SaveTokens = true;
            options.RequireHttpsMetadata = false;
            // TODO: options.RequireHttpsMetadata = true;
            options.GetClaimsFromUserInfoEndpoint = true;

            // Add required scopes
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("offline_access");

            // Ensure Authority/MetadataAddress use a single valid scheme (http or https),
            // avoiding unsupported "https+http" logical scheme values from service discovery.
            var keycloakBase = builder.Configuration[$"services:keycloak:https:0"]
                               ?? builder.Configuration[$"services:keycloak:http:0"];
            if (!string.IsNullOrWhiteSpace(keycloakBase))
            {
                // Keycloak realm endpoints follow /realms/{realm}
                options.Authority = $"{keycloakBase}/realms/blink";
                options.MetadataAddress = $"{keycloakBase}/realms/blink/.well-known/openid-configuration";
            }
        });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

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

app.Run();