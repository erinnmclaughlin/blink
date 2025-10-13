using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

namespace Blink.AppHost;

public static class PostgresServerDefaults
{
    public static IResourceBuilder<AzurePostgresFlexibleServerResource> AddAndConfigurePostgresServer(this IDistributedApplicationBuilder builder)
    {
        var pgUsername = builder.AddParameter("pg-username", secret: true);
        var pgPassword = builder.AddParameter("pg-password", secret: true);

        var postgres = builder
            .AddAzurePostgresFlexibleServer("pg-server")
            .WithPasswordAuthentication(pgUsername, pgPassword);

        if (builder.Environment.IsDevelopment())
        {
            postgres.RunAsContainer();
        }

        return postgres;
    }
}
