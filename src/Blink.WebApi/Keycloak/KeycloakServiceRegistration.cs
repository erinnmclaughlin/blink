using Blink.WebApi.Keycloak;
using MassTransit.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class KeycloakServiceRegistration
{
    public static void AddAndConfigureAuthentication(this WebApplicationBuilder builder)
    {
        builder.Services.AddAuthorization();

        builder.Services.AddAuthentication()
            .AddKeycloakJwtBearer("keycloak", "blink", o =>
            {
                o.Audience = "account";
                o.RequireHttpsMetadata = false;
                // TODO: o.RequireHttpsMetadata = builder.Environment.IsProduction();
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

