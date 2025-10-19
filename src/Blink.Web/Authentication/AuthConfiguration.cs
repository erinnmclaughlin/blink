using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Blink.Web.Authentication;

public static class AuthConfiguration
{
    public static void AddBlinkAuthorization(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddCascadingAuthenticationState()
            .AddHttpContextAccessor()
            .AddScoped<ICurrentUser>(sp => CurrentUser.FromClaims(sp.GetRequiredService<IHttpContextAccessor>().HttpContext!.User.Claims.ToList()))
            .AddScoped<AuthenticationStateProvider, RevalidatingAuthenticationStateProvider>()
            .Configure<AuthenticationOptions>(builder.Configuration.GetSection("Authentication"))
            .AddAuthorization()
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

                    // TODO: Can we use service discovery here? Seems to break WASM render mode.
                    var keycloakBase = builder.Configuration.GetHttpsEndpoint("keycloak") ??
                                       builder.Configuration.GetHttpEndpoint("keycloak");
                    if (!string.IsNullOrWhiteSpace(keycloakBase))
                    {
                        options.Authority = $"{keycloakBase}/realms/blink";
                        options.MetadataAddress = $"{keycloakBase}/realms/blink/.well-known/openid-configuration";
                    }
                });
    }

    public static void MapLoginLogoutEndpoints(this WebApplication app)
    {
        app.MapGet("/login", () => Results
                .Challenge(new AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme]))
            .AllowAnonymous();

        app.MapPost("/logout", () => Results.SignOut(new AuthenticationProperties { RedirectUri = "/" }, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
            .RequireAuthorization();
    }
}
