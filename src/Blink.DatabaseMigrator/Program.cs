using Blink.DatabaseMigrator;
using FluentMigrator.Runner;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services
    .AddFluentMigratorCore()
    .ConfigureRunner(rb =>
    {
        rb.AddPostgres();
        rb.WithGlobalConnectionString(builder.Configuration.GetConnectionString("blink-db"));
        rb.ScanIn(typeof(Program).Assembly).For.All();
    })
    .AddLogging(lb => lb.AddFluentMigratorConsole());

var host = builder.Build();

host.Run();
