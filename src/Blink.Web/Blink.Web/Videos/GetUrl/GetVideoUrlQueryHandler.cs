using Blink.Storage;
using Blink.VideosApi.Contracts.GetUrl;
using Dapper;
using MediatR;
using Npgsql;

namespace Blink.Web.Videos.GetUrl;

public sealed class GetVideoUrlQueryHandler : IRequestHandler<GetVideoUrlQuery, VideoUrlResponse>, IDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly IVideoStorageClient _videoStorageClient;

    public GetVideoUrlQueryHandler(NpgsqlDataSource dataSource, IVideoStorageClient videoStorageClient)
    {
        _connection = dataSource.CreateConnection();
        _videoStorageClient = videoStorageClient;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public async Task<VideoUrlResponse> Handle(GetVideoUrlQuery request, CancellationToken cancellationToken)
    {
        await _connection.OpenAsync(cancellationToken);

        var thumbnailBlobName = await GetThumnailBlobName(request.BlobName);
        
        return new VideoUrlResponse 
        { 
            Url = await _videoStorageClient.GetUrlAsync(request.BlobName, cancellationToken),
            ThumbnailUrl = thumbnailBlobName is null ? null : await _videoStorageClient.GetThumbnailUrlAsync(thumbnailBlobName, cancellationToken)
        };
    }

    private async Task<string?> GetThumnailBlobName(string videoBlobName)
    {
        const string sql = "select thumbnail_blob_name from videos where blob_name = @blobName";
        return await _connection.QuerySingleOrDefaultAsync<string?>(sql, new { blobName = videoBlobName });
    }
}
