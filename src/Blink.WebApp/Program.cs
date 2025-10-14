using Blink;
using Blink.WebApp;
using Blink.WebApp.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(); 
builder.Services.AddControllers();

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
            // TODO: options.RequireHttpsMetadata = builder.Environment.IsProduction();
            options.GetClaimsFromUserInfoEndpoint = true;

            // Add required scopes
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("profile");
            options.Scope.Add("email");
            options.Scope.Add("offline_access");
        });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Configure HTTP client for Blink API with authentication
var apiBaseAddress = builder.Configuration[$"services:{ServiceNames.BlinkWebApi}:https:0"] ??
                     builder.Configuration[$"services:{ServiceNames.BlinkWebApi}:http:0"] ??
                     "localhost";

builder.Services.AddTransient<BlinkApiAuthenticationHandler>();
builder.Services.AddHttpClient<BlinkApiClient>((sp, client) =>
{
    client.BaseAddress = new Uri(apiBaseAddress);
    client.Timeout = TimeSpan.FromMinutes(15); // for now, video processing can take a while
})
.AddHttpMessageHandler<BlinkApiAuthenticationHandler>();

builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 2_000_000_000; // 2GB
});

builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 2_000_000_000; // 2GB
});

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.UseAuthentication();

// Add token refresh middleware after authentication but before authorization
// This ensures tokens are refreshed before the response starts
app.UseMiddleware<TokenRefreshMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapGet("/login", () => Results
    .Challenge(new AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme]))
    .AllowAnonymous();

app.MapPost("/logout", (HttpContext context) => Results.SignOut(new AuthenticationProperties { RedirectUri = "/" }, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
    .RequireAuthorization();

app.Run();
