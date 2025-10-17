using FluentMigrator.Runner;

namespace Blink.DatabaseMigrator;

public sealed class Worker : BackgroundService
{
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IServiceProvider _services;
    
    public Worker(IHostApplicationLifetime appLifetime, IServiceProvider services)
    {
        _appLifetime = appLifetime;
        _services = services;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        
        await using var scope = _services.CreateAsyncScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
        
        _appLifetime.StopApplication();
    }
}