using Microsoft.Extensions.Hosting;

namespace Blink.AppHost;

public static class PostgresDatabaseSetup
{
    public static IResourceBuilder<PostgresServerResource> AddDefaultPostgresServer(this IDistributedApplicationBuilder builder)
    {
        var pgUsername = builder.AddParameter("pg-username", secret: true);
        var pgPassword = builder.AddParameter("pg-password", secret: true);
        var server = builder.AddPostgres("pg-server", pgUsername, pgPassword);

        if (builder.Environment.IsDevelopment())
        {
            server = server
                .WithDataVolume()
                .WithPgWeb();
        }
        
        return server;
    }
}