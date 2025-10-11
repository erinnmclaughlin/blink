using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Blink.WebApi.Videos;

public interface IVideoStorageClient
{
    Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default);
    Task<string> GetUrlAsync(string blobName, CancellationToken cancellationToken = default);
    Task<List<VideoInfo>> ListAsync(CancellationToken cancellationToken = default);
    Task<(string BlobName, long FileSize)> UploadAsync(Stream videoStream, string fileName, CancellationToken cancellationToken = default);
    Task<bool> UpdateTitleAsync(string blobName, string title, CancellationToken cancellationToken = default);
}

public class VideoStorageClient : IVideoStorageClient
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<VideoStorageClient> _logger;

    public VideoStorageClient(
        BlobServiceClient blobServiceClient,
        ILogger<VideoStorageClient> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = await GetBlobClientAsync(blobName, cancellationToken);

            var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);

            if (response.Value)
            {
                _logger.LogInformation("Video deleted successfully: {BlobName}", blobName);
            }
            else
            {
                _logger.LogWarning("Video not found for deletion: {BlobName}", blobName);
            }

            return response.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting video: {BlobName}", blobName);
            throw;
        }
    }

    public async Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = await GetBlobClientAsync(blobName, cancellationToken);

            var response = await blobClient.DownloadAsync(cancellationToken);

            _logger.LogInformation("Video downloaded successfully: {BlobName}", blobName);
            return response.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading video: {BlobName}", blobName);
            throw;
        }
    }

    public async Task<string> GetUrlAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = await GetBlobClientAsync(blobName, cancellationToken);

            // Check if blob exists
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Video not found: {blobName}");
            }

            // Generate SAS token valid for 1 hour
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = "videos",
                    BlobName = blobName,
                    Resource = "b", // b for blob
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5), // Allow for clock skew
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                _logger.LogInformation("Generated SAS URL for video: {BlobName}", blobName);
                return sasUri.ToString();
            }
            else
            {
                // If we can't generate SAS (using connection string without key), return the blob URI
                // This scenario would need anonymous access or different auth strategy
                _logger.LogWarning("Cannot generate SAS token for blob: {BlobName}. Returning direct URI: {Uri}", blobName, blobClient.Uri);
                
                // For local development with Azurite, the URI should still work
                return blobClient.Uri.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating video URL: {BlobName}", blobName);
            throw;
        }
    }

    public async Task<List<VideoInfo>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("videos");

            // Check if container exists
            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                _logger.LogInformation("Videos container does not exist yet");
                return [];
            }

            var videos = new List<VideoInfo>();

            await foreach (var blobItem in containerClient.GetBlobsAsync(traits: BlobTraits.Metadata, cancellationToken: cancellationToken))
            {
                // Extract original filename from blob name (format: guid_filename)
                var fileName = blobItem.Name;
                var underscoreIndex = fileName.IndexOf('_');
                if (underscoreIndex > 0 && underscoreIndex < fileName.Length - 1)
                {
                    fileName = fileName[(underscoreIndex + 1)..];
                }

                // Get title from metadata, or use filename as fallback
                string? title = null;
                if (blobItem.Metadata != null && blobItem.Metadata.TryGetValue("title", out var metadataTitle))
                {
                    title = metadataTitle;
                }

                videos.Add(new VideoInfo(
                    BlobName: blobItem.Name,
                    FileName: fileName,
                    SizeInBytes: blobItem.Properties.ContentLength ?? 0,
                    LastModified: blobItem.Properties.LastModified,
                    ContentType: blobItem.Properties.ContentType ?? "video/mp4",
                    Title: title,
                    Description: null,
                    VideoDate: null,
                    OwnerId: string.Empty // Legacy method - owner info not available in blob storage
                ));
            }

            _logger.LogInformation("Retrieved {Count} videos from blob storage", videos.Count);
            return videos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing videos");
            throw;
        }
    }

    public async Task<(string BlobName, long FileSize)> UploadAsync(Stream videoStream, string fileName, CancellationToken cancellationToken = default)
    {
        var blobName = $"{Guid.NewGuid()}_{fileName}";

        try
        {
            var blobClient = await GetBlobClientAsync(blobName, cancellationToken);

            var response = await blobClient.UploadAsync(
                videoStream,
                new BlobUploadOptions { HttpHeaders = new() { ContentType = GetContentType(fileName) } },
                cancellationToken);

            // Get the actual uploaded size from the response
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var fileSize = properties.Value.ContentLength;

            _logger.LogInformation("Video uploaded successfully: {BlobName}, Size: {FileSize} bytes", blobName, fileSize);
            return (blobName, fileSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading video: {FileName}", fileName);
            throw;
        }
    }

    private async Task<BlobClient> GetBlobClientAsync(string blobName, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("videos");
        await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
        return containerClient.GetBlobClient(blobName);
    }

    public async Task<bool> UpdateTitleAsync(string blobName, string title, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = await GetBlobClientAsync(blobName, cancellationToken);

            // Check if blob exists
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Video not found for title update: {BlobName}", blobName);
                return false;
            }

            // Get current metadata
            var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
            var metadata = properties.Value.Metadata;

            // Update or add title metadata
            metadata["title"] = title;

            // Set the updated metadata
            await blobClient.SetMetadataAsync(metadata, cancellationToken: cancellationToken);

            _logger.LogInformation("Video title updated successfully: {BlobName}, Title: {Title}", blobName, title);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating video title: {BlobName}", blobName);
            throw;
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".avi" => "video/x-msvideo",
            ".wmv" => "video/x-ms-wmv",
            _ => "application/octet-stream"
        };
    }
}

public record VideoInfo(
    string BlobName,
    string FileName,
    long SizeInBytes,
    DateTimeOffset? LastModified,
    string ContentType,
    string? Title,
    string? Description,
    DateTime? VideoDate,
    string OwnerId
);