namespace Blink.AppHost;

public static class KeycloakDefaults
{
    public static IResourceBuilder<KeycloakResource> AddAndConfigureKeycloak(this IDistributedApplicationBuilder builder, PostgresServerResources postgres)
    {
        var keycloakPassword = builder.AddParameter("keycloak-password", secret: true);

        var keycloak = builder.AddKeycloak("keycloak", adminPassword: keycloakPassword);
        
        if (builder.ExecutionContext.IsRunMode)
        {
            keycloak.WithDataVolume().WithLifetime(ContainerLifetime.Persistent);
        }
        else
        {
            var keycloakDb = postgres.Server.AddDatabase("keycloak-db", "keycloak");
            var keycloakDbUrl = ReferenceExpression.Create(
                $"jdbc:postgresql://{postgres.Server.GetOutput("hostName")}/{keycloakDb.Resource.DatabaseName}"
            );

            keycloak
                .WithEnvironment("KC_HTTP_ENABLED", "true")
                .WithEnvironment("KC_PROXY_HEADERS", "xforwarded")
                .WithEnvironment("KC_HOSTNAME_STRICT", "false")
                .WithEnvironment("KC_DB", "postgres")
                .WithEnvironment("KC_DB_URL", keycloakDbUrl)
                .WithEnvironment("KC_DB_USERNAME", postgres.PostgresUser)
                .WithEnvironment("KC_DB_PASSWORD", postgres.PostgresPassword)
                .WithEndpoint("http", e => e.IsExternal = true);
        }
        
        return keycloak;
    }
}