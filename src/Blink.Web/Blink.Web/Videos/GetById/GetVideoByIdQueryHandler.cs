using Blink.VideosApi.Contracts.GetById;
using Dapper;
using MediatR;
using Npgsql;

namespace Blink.Web.Videos.GetById;

public sealed class GetVideoByIdQueryHandler : IRequestHandler<GetVideoByIdQuery, VideoDetailDto?>, IDisposable
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

    public async Task<VideoDetailDto?> Handle(GetVideoByIdQuery request, CancellationToken cancellationToken)
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
            WHERE id = @Id
            """;

        return await _connection.QuerySingleOrDefaultAsync<VideoDetailDto>(sql, new { Id = request.Id });
    }
}
