using FluentMigrator.Runner;

namespace Blink.DatabaseMigrator.Migrations;

public static class FluentMigrationServiceRegistration
{
    public static void AddAndConfigureFluentMigrations(this IHostApplicationBuilder builder)
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
