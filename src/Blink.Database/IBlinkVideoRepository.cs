using Blink.Videos;
using Dapper;
using Npgsql;

namespace Blink.Database;

public interface IBlinkVideoRepository
{
    void Add(BlinkVideo video);
    void UpdateMetaData(Guid videoId, BlinkVideoMetaData metadata);
    void UpdateThumbnail(Guid videoId, string thumbnailBlobName);
}

internal sealed class BlinkVideoRepository : IBlinkVideoRepository
{
    private readonly IDateProvider _dateProvider;

    private readonly List<(string Sql, object? Parameters)> _pendingSqlCommands = [];

    public BlinkVideoRepository(IDateProvider dateProvider)
    {
        _dateProvider = dateProvider;
    }
    
    public void Add(BlinkVideo video)
    {
        _pendingSqlCommands.Add((SqlConstants.Insert, new
        {
            id = video.Id,
            blob_name = video.File.BlobName,
            title = video.Title,
            description = video.Description,
            video_date = video.CaptureDate,
            file_name = video.File.FileName,
            content_type = video.File.ContentType,
            size_in_bytes = video.File.FileSize,
            owner_id = video.OwnerId,
            uploaded_at = video.UploadedAt,
            updated_at = _dateProvider.UtcNow
        }));
    }

    public void UpdateMetaData(Guid videoId, BlinkVideoMetaData metadata)
    {
        _pendingSqlCommands.Add((SqlConstants.UpdateMetaData, new
        {
            id = videoId,
            duration_in_sections = metadata.Duration?.TotalSeconds,
            width = metadata.AspectRatio?.Width,
            height = metadata.AspectRatio?.Height,
            updated_at = _dateProvider.UtcNow
        }));
    }

    public void UpdateThumbnail(Guid videoId, string thumbnailBlobName)
    {
        _pendingSqlCommands.Add((SqlConstants.UpdateThumbnail, new
        {
            id = videoId,
            thumbnail_blob_name = thumbnailBlobName,
            updated_at = _dateProvider.UtcNow
        }));
    }
    
    internal async Task<int> SaveChangesAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        var affectedRowCount = 0;

        for (var i = _pendingSqlCommands.Count - 1; i >= 0; i--)
        {
            var (sql, parameters) = _pendingSqlCommands[i];
            
            cancellationToken.ThrowIfCancellationRequested();
            affectedRowCount += await connection.ExecuteAsync(sql, parameters);
            
            _pendingSqlCommands.RemoveAt(i);
        }
        
        return affectedRowCount;
    }

    private static class SqlConstants
    {
        public const string Insert = 
            """
            INSERT INTO videos (
                id,
                blob_name, 
                title, 
                description, 
                /*description_mentions, */
                video_date, 
                file_name, 
                content_type, 
                size_in_bytes, 
                owner_id, 
                uploaded_at,
                updated_at
            ) VALUES (
                @id, 
                @blob_name, 
                @title,
                @description,
                /*@description_mentions::jsonb, */
                @video_date,
                @file_name, 
                @content_type, 
                @size_in_bytes,
                @owner_id,
                @uploaded_at,
                @updated_at
            )
            """;

        public const string UpdateMetaData =
            """
            UPDATE videos
            SET width = @width,
                height = @height,
                duration_in_seconds = @duration_in_seconds,
                updated_at = @updated_at
            WHERE id = @id
            """;

        public const string UpdateThumbnail =
            """
            UPDATE videos
            SET thumbnail_blob_name = @thumbnail_blob_name,
                updated_at = @updated_at
            WHERE id = @id
            """;
    }
}
