using Dapper;
using MediatR;
using Npgsql;

namespace Blink.Web.Videos.Requests;

public static class GetVideoDetails
{
    public sealed record Query(Guid VideoId) : IRequest<Video?>;

    public sealed record Video
    {
        public required Guid Id { get; init; }
        public required string BlobName { get; init; }
        public required string Title { get; init; }
        public string? Description { get; init; }
        public DateOnly? VideoDate { get; init; }
        public string? ThumbnailBlobName { get; init; }
        public DateTimeOffset UploadedAt { get; init; }
    }
    
    public sealed class QueryHandler : IRequestHandler<Query, Video?>, IDisposable
    {
        private readonly NpgsqlConnection _connection;

        public QueryHandler(NpgsqlDataSource dataSource)
        {
            _connection = dataSource.CreateConnection();
        }

        public void Dispose()
        {
            _connection.Dispose();
        }

        public async Task<Video?> Handle(Query request, CancellationToken cancellationToken)
        {
            await _connection.OpenAsync(cancellationToken);

            const string sql = """
                SELECT 
                    id,
                    blob_name,
                    title, 
                    description,
                    video_date,
                    thumbnail_blob_name,
                    uploaded_at
                FROM videos
                WHERE id = @VideoId
                """;

            var row = await _connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { request.VideoId });
            
            return row is null ? null : new Video
            {
                Id = row.id,
                BlobName = row.blob_name,
                Title = row.title,
                Description = row.description,
                VideoDate = row.video_date != null ? DateOnly.FromDateTime((DateTime)row.video_date) : null,
                ThumbnailBlobName = row.thumbnail_blob_name,
                UploadedAt = row.uploaded_at
            };
        }
    }
}
