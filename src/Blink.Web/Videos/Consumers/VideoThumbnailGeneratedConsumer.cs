using Blink.Messaging;
using Dapper;
using MassTransit;
using Npgsql;

namespace Blink.Web.Videos.Consumers;

public sealed class VideoThumbnailGeneratedConsumer : IConsumer<VideoThumbnailGenerated>
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IDateProvider _dateProvider;

    public VideoThumbnailGeneratedConsumer(NpgsqlDataSource dataSource, IDateProvider dateProvider)
    {
        _dataSource = dataSource;
        _dateProvider = dateProvider;
    }

    public async Task Consume(ConsumeContext<VideoThumbnailGenerated> context)
    {
        const string sql = """
            UPDATE videos
            SET thumbnail_blob_name = @ThumbnailBlobName,
                updated_at = @UpdatedAt
            WHERE blob_name = @BlobName
            """;

        await using var connection = await _dataSource.OpenConnectionAsync(context.CancellationToken);
        await connection.ExecuteAsync(sql, new
        {
            BlobName = context.Message.VideoBlobName,
            ThumbnailBlobName = context.Message.ThumbnailBlobName,
            UpdatedAt = _dateProvider.UtcNow
        });
    }
}
