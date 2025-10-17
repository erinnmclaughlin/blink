using Blink.Messaging;
using Dapper;
using MassTransit;
using Npgsql;

namespace Blink.Web.EventConsumers;

public sealed class VideoMetadataExtractedConsumer : IConsumer<VideoMetadataExtracted>, IDisposable
{
    private readonly NpgsqlConnection _connection;

    public VideoMetadataExtractedConsumer(NpgsqlDataSource dataSource)
    {
        _connection = dataSource.CreateConnection();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public async Task Consume(ConsumeContext<VideoMetadataExtracted> context)
    {
        const string sql = """
            UPDATE videos
            SET width = @Width,
                height = @Height,
                duration_in_seconds = @DurationInSeconds,
                updated_at = @UpdatedAt
            WHERE blob_name = @BlobName
            """;

        await _connection.OpenAsync(context.CancellationToken);
        await _connection.ExecuteAsync(sql, new
        {
            BlobName = context.Message.VideoBlobName,
            Width = context.Message.Metadata.Width,
            Height = context.Message.Metadata.Height,
            DurationInSeconds = context.Message.Metadata.DurationInSeconds,
            UpdatedAt = DateTimeOffset.UtcNow
        });
    }
}
