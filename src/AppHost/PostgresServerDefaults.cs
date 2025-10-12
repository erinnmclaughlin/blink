namespace Blink.AppHost;

public static class PostgresServerDefaults
{
    public static IResourceBuilder<PostgresServerResource> AddAndConfigurePostgresServer(this IDistributedApplicationBuilder builder)
    {
        var pgUsername = builder.AddParameter("pg-username", secret: true);
        var pgPassword = builder.AddParameter("pg-password", secret: true);

        return builder
            .AddPostgres("pg-server", pgUsername, pgPassword)
            .WithDataVolume()
            .WithPgWeb();
    }
}
