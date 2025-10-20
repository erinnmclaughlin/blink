using Npgsql;

namespace Blink.Database;

public interface IBlinkUnitOfWork
{
    IBlinkVideoRepository Videos { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

internal sealed class BlinkUnitOfWork : IBlinkUnitOfWork
{
    private readonly NpgsqlDataSource _dataSource;

    private readonly BlinkVideoRepository _videos;
    public IBlinkVideoRepository Videos => _videos;

    public BlinkUnitOfWork(NpgsqlDataSource dataSource, IDateProvider dateProvider)
    {
        _dataSource = dataSource;
        _videos = new BlinkVideoRepository(dateProvider);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var affectedRowCount = 0;

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        affectedRowCount += await _videos.SaveChangesAsync(connection, cancellationToken);
        
        return affectedRowCount;
    }
}
