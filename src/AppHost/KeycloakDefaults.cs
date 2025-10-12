namespace Blink.AppHost;

public static class KeycloakDefaults
{
    public static IResourceBuilder<KeycloakResource> AddAndConfigureKeycloak(this IDistributedApplicationBuilder builder)
    {
        return builder
            .AddKeycloak("keycloak", 8080)
            .WithExternalHttpEndpoints()
            .WithDataVolume();
    }
}