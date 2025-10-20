using Microsoft.Extensions.DependencyInjection;

namespace Blink.Database;

public interface IBlinkUnitOfWorkFactory
{
    IBlinkUnitOfWork CreateUnitOfWork();
}

internal sealed class BlinkUnitOfWorkFactory : IBlinkUnitOfWorkFactory
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public BlinkUnitOfWorkFactory(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public IBlinkUnitOfWork CreateUnitOfWork()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IBlinkUnitOfWork>();
    }
}
