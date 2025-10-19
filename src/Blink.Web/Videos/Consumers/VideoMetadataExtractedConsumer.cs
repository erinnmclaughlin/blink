using Blink.Messaging;
using Dapper;
using MassTransit;
using Npgsql;

namespace Blink.Web.Videos.Consumers;

public sealed class VideoMetadataExtractedConsumer : IConsumer<VideoMetadataExtracted>
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IDateProvider _dateProvider;

    public VideoMetadataExtractedConsumer(NpgsqlDataSource dataSource, IDateProvider dateProvider)
    {
        _dataSource = dataSource;
        _dateProvider = dateProvider;
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

        await using var connection = await _dataSource.OpenConnectionAsync(context.CancellationToken);
        await connection.ExecuteAsync(sql, new
        {
            BlobName = context.Message.VideoBlobName,
            Width = context.Message.Metadata.Width,
            Height = context.Message.Metadata.Height,
            DurationInSeconds = context.Message.Metadata.DurationInSeconds,
            UpdatedAt = _dateProvider.UtcNow
        });
    }
}
