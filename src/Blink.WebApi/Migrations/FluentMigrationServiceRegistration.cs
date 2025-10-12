using Blink;
using FluentMigrator.Runner;

namespace Microsoft.Extensions.DependencyInjection;

public static class FluentMigrationServiceRegistration
{
    public static void AddAndConfigureFluentMigrations(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddFluentMigratorCore()
            .ConfigureRunner(rb =>
            {
                rb.AddPostgres();
                rb.WithGlobalConnectionString(builder.Configuration.GetConnectionString(ServiceNames.BlinkDatabase));
                rb.ScanIn(typeof(Program).Assembly).For.All();
            })
            .AddLogging(lb => lb.AddFluentMigratorConsole());
    }
}
