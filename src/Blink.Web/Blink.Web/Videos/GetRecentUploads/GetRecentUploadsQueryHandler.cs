using Blink.VideosApi.Contracts;
using Blink.VideosApi.Contracts.GetRecentUploads;
using Dapper;
using MediatR;
using Npgsql;

namespace Blink.Web.Videos.GetRecentUploads;

internal sealed class GetRecentUploadsQueryHandler : IRequestHandler<GetRecentUploadsQuery, List<VideoSummaryDto>>
{
    private readonly NpgsqlDataSource _dataSource;

    public GetRecentUploadsQueryHandler(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<VideoSummaryDto>> Handle(GetRecentUploadsQuery request, CancellationToken cancellationToken)
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
        var videos = await connection.QueryAsync<VideoSummaryDto>(sql);
        return [.. videos];
    }
}
