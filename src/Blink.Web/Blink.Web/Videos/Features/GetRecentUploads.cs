using Dapper;
using MediatR;
using Npgsql;

namespace Blink.Web.Videos.Features;

public static class GetRecentUploads
{
    public sealed record Query : IRequest<List<Video>>;

    public sealed record Video
    {
        public required Guid Id { get; init; }
        public required string BlobName { get; init; }
        public required string Title { get; init; }
        public required double? DurationInSeconds { get; init; }
        public required long SizeInBytes { get; init; }
        public required string? ThumbnailBlobName { get; init; }
        public required DateTimeOffset UploadedAt { get; init; }
        public required DateOnly? VideoDate { get; init; }
    }
    
    public sealed class QueryHandler : IRequestHandler<Query, List<Video>>
    {
        private readonly NpgsqlDataSource _dataSource;

        public QueryHandler(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<List<Video>> Handle(Query request, CancellationToken cancellationToken)
        {
            const string sql = """
                               SELECT
                                   id,
                                   blob_name,
                                   title,
                                   description,
                                   duration_in_seconds,
                                   size_in_bytes,
                                   thumbnail_blob_name,
                                   uploaded_at,
                                   video_date
                               FROM videos
                               ORDER BY uploaded_at DESC
                               LIMIT 12
                               """;

            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var videos = await connection.QueryAsync<Video>(sql);
            return [.. videos];
        }
    }
}