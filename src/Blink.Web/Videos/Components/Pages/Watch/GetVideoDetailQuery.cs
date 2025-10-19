using System.Text.Json;
using Blink.Web.Mentions;
using Dapper;
using MediatR;
using Npgsql;

namespace Blink.Web.Videos.Pages.Watch;

public sealed record GetVideoDetailQuery(Guid VideoId) : IRequest<VideoDetailVm?>;

public sealed class GetVideoByIdQueryHandler : IRequestHandler<GetVideoDetailQuery, VideoDetailVm?>, IDisposable
{
    private readonly NpgsqlConnection _connection;

    public GetVideoByIdQueryHandler(NpgsqlDataSource dataSource)
    {
        _connection = dataSource.CreateConnection();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public async Task<VideoDetailVm?> Handle(GetVideoDetailQuery request, CancellationToken cancellationToken)
    {
        await _connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT 
                id,
                blob_name,
                title, 
                description,
                description_mentions,
                video_date,
                thumbnail_blob_name,
                uploaded_at
            FROM videos
            WHERE id = @VideoId
            """;

        var row = await _connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { request.VideoId });
        
        if (row == null)
        {
            return null;
        }

        // Deserialize mention metadata from JSON
        List<MentionMetadata>? mentions = null;
        if (row.description_mentions != null)
        {
            try
            {
                var json = (string)row.description_mentions;
                mentions = JsonSerializer.Deserialize<List<MentionMetadata>>(json);
            }
            catch
            {
                // If deserialization fails, mentions will be null and won't be styled
            }
        }

        return new VideoDetailVm
        {
            Id = row.id,
            BlobName = row.blob_name,
            Title = row.title,
            Description = row.description,
            DescriptionMentions = mentions,
            VideoDate = row.video_date != null ? DateOnly.FromDateTime((DateTime)row.video_date) : null,
            ThumbnailBlobName = row.thumbnail_blob_name,
            UploadedAt = row.uploaded_at
        };
    }
}
