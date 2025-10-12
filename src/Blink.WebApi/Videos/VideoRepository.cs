using Dapper;
using Npgsql;

namespace Blink.WebApi.Videos;

/// <summary>
/// Repository implementation for video database operations using Dapper
/// </summary>
public sealed class VideoRepository : IVideoRepository
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<VideoRepository> _logger;

    static VideoRepository()
    {
        // Configure Dapper to map snake_case columns to PascalCase properties
        DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public VideoRepository(NpgsqlDataSource dataSource, ILogger<VideoRepository> logger)
    {
        _dataSource = dataSource;
        _logger = logger;
    }

    public async Task<Video> CreateAsync(Video video, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO videos (id, blob_name, title, description, video_date, file_name, content_type, size_in_bytes, owner_id, uploaded_at, updated_at, thumbnail_blob_name, width, height, duration_in_seconds)
            VALUES (@Id, @BlobName, @Title, @Description, @VideoDate, @FileName, @ContentType, @SizeInBytes, @OwnerId, @UploadedAt, @UpdatedAt, @ThumbnailBlobName, @Width, @Height, @DurationInSeconds)
            RETURNING *
            """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var result = await connection.QuerySingleAsync<Video>(sql, video);
            
            _logger.LogInformation("Created video record: {BlobName}, Owner: {OwnerId}", video.BlobName, video.OwnerId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating video record: {BlobName}", video.BlobName);
            throw;
        }
    }

    public async Task<Video?> GetByBlobNameAsync(string blobName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, blob_name, title, description, video_date, file_name, content_type, size_in_bytes, owner_id, uploaded_at, updated_at, thumbnail_blob_name, width, height, duration_in_seconds
            FROM videos
            WHERE blob_name = @BlobName
            """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<Video>(sql, new { BlobName = blobName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video by blob name: {BlobName}", blobName);
            throw;
        }
    }

    public async Task<List<Video>> GetByOwnerIdAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, blob_name, title, description, video_date, file_name, content_type, size_in_bytes, owner_id, uploaded_at, updated_at, thumbnail_blob_name, width, height, duration_in_seconds
            FROM videos
            WHERE owner_id = @OwnerId
            ORDER BY uploaded_at DESC
            """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var results = await connection.QueryAsync<Video>(sql, new { OwnerId = ownerId });
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting videos by owner: {OwnerId}", ownerId);
            throw;
        }
    }

    public async Task<List<Video>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT id, blob_name, title, description, video_date, file_name, content_type, size_in_bytes, owner_id, uploaded_at, updated_at, thumbnail_blob_name, width, height, duration_in_seconds
            FROM videos
            ORDER BY uploaded_at DESC
            """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var results = await connection.QueryAsync<Video>(sql);
            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all videos");
            throw;
        }
    }

    public async Task<bool> UpdateAsync(Video video, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE videos
            SET title = @Title,
                description = @Description,
                video_date = @VideoDate,
                updated_at = @UpdatedAt
            WHERE blob_name = @BlobName
            """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var rowsAffected = await connection.ExecuteAsync(sql, video);
            
            _logger.LogInformation("Updated video record: {BlobName}, Rows affected: {RowsAffected}", video.BlobName, rowsAffected);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating video record: {BlobName}", video.BlobName);
            throw;
        }
    }

    public async Task<bool> DeleteByBlobNameAsync(string blobName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            DELETE FROM videos
            WHERE blob_name = @BlobName
            """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var rowsAffected = await connection.ExecuteAsync(sql, new { BlobName = blobName });
            
            _logger.LogInformation("Deleted video record: {BlobName}, Rows affected: {RowsAffected}", blobName, rowsAffected);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting video record: {BlobName}", blobName);
            throw;
        }
    }

    public async Task<bool> UpdateThumbnailAsync(string blobName, string thumbnailBlobName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE videos
            SET thumbnail_blob_name = @ThumbnailBlobName,
                updated_at = @UpdatedAt
            WHERE blob_name = @BlobName
            """;

        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            var rowsAffected = await connection.ExecuteAsync(sql, new 
            { 
                BlobName = blobName, 
                ThumbnailBlobName = thumbnailBlobName,
                UpdatedAt = DateTime.UtcNow
            });
            
            _logger.LogInformation("Updated video thumbnail: {BlobName}, Thumbnail: {ThumbnailBlobName}, Rows affected: {RowsAffected}", 
                blobName, thumbnailBlobName, rowsAffected);
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating video thumbnail: {BlobName}", blobName);
            throw;
        }
    }
}

