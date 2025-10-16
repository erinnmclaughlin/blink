using Blink.Messaging;

namespace Blink.Web.Videos;

/// <summary>
/// Repository interface for video database operations
/// </summary>
public interface IVideoRepository
{
    /// <summary>
    /// Creates a new video record in the database
    /// </summary>
    Task<Video> CreateAsync(Video video, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a video by its blob name
    /// </summary>
    Task<Video?> GetByBlobNameAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all videos for a specific owner
    /// </summary>
    Task<List<Video>> GetByOwnerIdAsync(string ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all videos (for admin purposes)
    /// </summary>
    Task<List<Video>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing video record
    /// </summary>
    Task<bool> UpdateAsync(Video video, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the metadata for a video
    /// </summary>
    Task UpdateMetadataAsync(string blobName, VideoMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a video record by blob name
    /// </summary>
    Task<bool> DeleteByBlobNameAsync(string blobName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the thumbnail blob name for a video
    /// </summary>
    Task<bool> UpdateThumbnailAsync(string blobName, string thumbnailBlobName, CancellationToken cancellationToken = default);
}