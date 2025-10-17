using Dapper;
using MediatR;
using Npgsql;

namespace Blink.Web.Components.Pages.Videos.Watch;

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
                video_date,
                thumbnail_blob_name,
                uploaded_at
            FROM videos
            WHERE id = @VideoId
            """;

        return await _connection.QuerySingleOrDefaultAsync<VideoDetailVm>(sql, new { request.VideoId });
    }
}
