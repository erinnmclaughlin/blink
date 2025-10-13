using Blink;
using Blink.BlazorSSR;
using Blink.BlazorSSR.Components;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

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
        });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Configure HTTP client for Blink API with authentication
var apiBaseAddress = builder.Configuration[$"services:{ServiceNames.BlinkWebApi}:https:0"] ?? 
                     builder.Configuration[$"services:{ServiceNames.BlinkWebApi}:http:0"] ??
                     "localhost";

builder.Services.AddHttpClient<BlinkApiClient>((sp, client) =>
{
    client.BaseAddress = new Uri(apiBaseAddress);
    client.Timeout = TimeSpan.FromMinutes(15); // for now, video processing can take a while
})
.AddHttpMessageHandler(sp =>
{
    return new BlinkApiAuthenticationHandler(sp);
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add authentication endpoints
app.MapGet("/login", () => Results.Challenge(
    new Microsoft.AspNetCore.Authentication.AuthenticationProperties 
    { 
        RedirectUri = "/" 
    },
    [OpenIdConnectDefaults.AuthenticationScheme]))
    .AllowAnonymous();

app.MapPost("/logout", (HttpContext context) =>
{
    return Results.SignOut(
        new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = "/"
        },
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
}).RequireAuthorization();

app.Run();

