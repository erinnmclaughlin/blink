using Blink.Messaging;
using Dapper;
using MassTransit;
using Npgsql;

namespace Blink.Web.Videos.Consumers;

public sealed class VideoThumbnailGeneratedConsumer : IConsumer<VideoThumbnailGenerated>, IDisposable
{
    private readonly NpgsqlConnection _connection;

    public VideoThumbnailGeneratedConsumer(NpgsqlDataSource dataSource)
    {
        _connection = dataSource.CreateConnection();
    }

    public void Dispose()
    {
        _connection.Dispose();

    }

    public async Task Consume(ConsumeContext<VideoThumbnailGenerated> context)
    {
        const string sql = """
            UPDATE videos
            SET thumbnail_blob_name = @ThumbnailBlobName,
                updated_at = @UpdatedAt
            WHERE blob_name = @BlobName
            """;

        await _connection.OpenAsync(context.CancellationToken);
        await _connection.ExecuteAsync(sql, new
        {
            BlobName = context.Message.VideoBlobName,
            ThumbnailBlobName = context.Message.ThumbnailBlobName,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
}
