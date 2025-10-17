using Blink;
using Blink.DatabaseMigrator;
using Blink.DatabaseMigrator.Migrations;
using Dapper;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.AddNpgsqlDataSource(ServiceNames.BlinkDatabase);
builder.AddAndConfigureFluentMigrations();

var host = builder.Build();

host.Run();