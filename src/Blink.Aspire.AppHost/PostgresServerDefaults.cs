using Aspire.Hosting.Azure;

namespace Blink.AppHost;

public static class PostgresServerDefaults
{
    public static PostgresServerResources AddAndConfigurePostgresServer(this IDistributedApplicationBuilder builder)
    {
        var pgUsername = builder.AddParameter("pg-username", value: "postgres");
        var pgPassword = builder.AddParameter("pg-password", secret: true);

        var postgres = builder
            .AddAzurePostgresFlexibleServer("pg-server")
            .WithPasswordAuthentication(pgUsername, pgPassword);

        if (builder.ExecutionContext.IsRunMode)
        {
            postgres.RunAsContainer(x =>
            {
                // x.WithHostPort(58488);
                x.WithDataVolume();
                x.WithPgWeb();
            });
        }

        return new PostgresServerResources
        {
            PostgresUser = pgUsername,
            PostgresPassword = pgPassword,
            Server = postgres
        };
    }
}

public sealed class PostgresServerResources
{
    public required IResourceBuilder<ParameterResource> PostgresUser { get; init; }
    public required IResourceBuilder<ParameterResource> PostgresPassword { get; init; }
    public required IResourceBuilder<AzurePostgresFlexibleServerResource> Server { get; init; }
}
