using Blink.WebApi.Keycloak;
using MassTransit.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class KeycloakServiceRegistration
{
    public static void AddAndConfigureAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorization();

        builder.Services.AddAuthentication()
            .AddKeycloakJwtBearer("keycloak", "blink", o =>
            {
                o.RequireHttpsMetadata = false;
                // TODO: o.RequireHttpsMetadata = builder.Environment.IsProduction();
                
                // Configure token validation to skip audience validation
                // This allows the blink-webapp client to call this API with its access token
                // The issuer (Keycloak realm) validation is still enforced
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });
    }

    public static void AddKeycloakEventPoller(this WebApplicationBuilder builder)
    {
        builder.Services
            .Configure<KeycloakOptions>(builder.Configuration.GetSection("Keycloak"))
            .AddHttpClient("keycloak", (sp, client) =>
            {
                var opt = sp.GetRequiredService<IOptions<KeycloakOptions>>().Value;
                client.BaseAddress = new Uri(opt.BaseUrl);
            });

        builder.Services.AddHostedService<KeycloakEventPoller>();
        builder.Services.Configure<HostOptions>(o =>
        {
            o.ServicesStartConcurrently = true;
            o.ServicesStopConcurrently = true;
        });
    }
}

