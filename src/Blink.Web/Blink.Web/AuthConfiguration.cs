using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Blink.Web;

public static class AuthConfiguration
{
    public static void AddAndConfigureAuth(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddCascadingAuthenticationState()
            .AddHttpContextAccessor()
            .AddScoped<AuthenticationStateProvider, RevalidatingAuthenticationStateProvider>()
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
                    var keycloakBase = builder.Configuration.GetHttpsEndpoint(ServiceNames.Keycloak) ??
                                       builder.Configuration.GetHttpEndpoint(ServiceNames.Keycloak);
                    if (!string.IsNullOrWhiteSpace(keycloakBase))
                    {
                        options.Authority = $"{keycloakBase}/realms/blink";
                        options.MetadataAddress = $"{keycloakBase}/realms/blink/.well-known/openid-configuration";
                    }
                });
    }
}