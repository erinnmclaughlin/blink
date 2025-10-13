using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;

namespace Blink.Storage;

public interface IVideoStorageClient
{
    Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default);
    Task<string> GetUrlAsync(string blobName, CancellationToken cancellationToken = default);
    Task<List<VideoInfo>> ListAsync(CancellationToken cancellationToken = default);
    Task<(string BlobName, long FileSize)> UploadAsync(Stream videoStream, string fileName, CancellationToken cancellationToken = default);
    Task<bool> UpdateTitleAsync(string blobName, string title, CancellationToken cancellationToken = default);
    Task<string> UploadThumbnailAsync(Stream thumbnailStream, string videoBlobName, CancellationToken cancellationToken = default);
    Task<string?> GetThumbnailUrlAsync(string thumbnailBlobName, CancellationToken cancellationToken = default);
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
            var blobClient = GetBlobClient(blobName);

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
            var blobClient = GetBlobClient(blobName);

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
            var blobClient = GetBlobClient(blobName);

            // Check if blob exists
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                throw new FileNotFoundException($"Video not found: {blobName}");
            }

            // Generate SAS token valid for 1 hour
            // When allowSharedKeyAccess is false (Azure production), we need user delegation SAS
            if (blobClient.CanGenerateSasUri)
            {
                // Account key-based SAS (local development with Azurite)
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
                _logger.LogInformation("Generated account key SAS URL for video: {BlobName}", blobName);
                return sasUri.ToString();
            }
            else
            {
                // User delegation SAS (Azure production with managed identity)
                _logger.LogInformation("Generating user delegation SAS token for blob: {BlobName}", blobName);
                
                var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
                var expiresOn = DateTimeOffset.UtcNow.AddHours(1);
                
                // Get a user delegation key
                var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(startsOn, expiresOn, cancellationToken);
                
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = "videos",
                    BlobName = blobName,
                    Resource = "b",
                    StartsOn = startsOn,
                    ExpiresOn = expiresOn
                };
                
                sasBuilder.SetPermissions(BlobSasPermissions.Read);
                
                var blobUriBuilder = new BlobUriBuilder(blobClient.Uri)
                {
                    Sas = sasBuilder.ToSasQueryParameters(userDelegationKey.Value, _blobServiceClient.AccountName)
                };
                
                _logger.LogInformation("Generated user delegation SAS URL for video: {BlobName}", blobName);
                return blobUriBuilder.ToUri().ToString();
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

            // Configure chunked upload for large files to prevent timeouts
            var uploadOptions = new BlobUploadOptions 
            { 
                HttpHeaders = new BlobHttpHeaders { ContentType = GetContentType(fileName) },
                TransferOptions = new Azure.Storage.StorageTransferOptions
                {
                    // Upload in 4MB chunks to improve reliability and prevent timeouts
                    InitialTransferSize = 4 * 1024 * 1024, // 4MB
                    MaximumTransferSize = 4 * 1024 * 1024,  // 4MB
                    MaximumConcurrency = 4 // Upload up to 4 chunks in parallel
                }
            };

            var response = await blobClient.UploadAsync(
                videoStream,
                uploadOptions,
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

    /// <summary>
    /// Gets a blob client for read operations (does not create container)
    /// </summary>
    private BlobClient GetBlobClient(string blobName)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("videos");
        return containerClient.GetBlobClient(blobName);
    }

    /// <summary>
    /// Gets a blob client and ensures the container exists (for write operations)
    /// </summary>
    private async Task<BlobClient> GetBlobClientAsync(string blobName, CancellationToken cancellationToken)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("videos");
        
        try
        {
            await containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 409)
        {
            // 409 Conflict can occur when:
            // 1. Container already exists (expected, can ignore)
            // 2. Public access is not permitted on this storage account (expected with allowBlobPublicAccess: false)
            // In both cases, we can safely proceed if the container exists
            _logger.LogDebug("Received 409 when creating container (container likely already exists): {Message}", ex.Message);
            
            // Verify the container actually exists
            if (!await containerClient.ExistsAsync(cancellationToken))
            {
                _logger.LogError("Container does not exist and cannot be created: {Message}", ex.Message);
                throw;
            }
        }
        
        return containerClient.GetBlobClient(blobName);
    }

    public async Task<bool> UpdateTitleAsync(string blobName, string title, CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = GetBlobClient(blobName);

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

    public async Task<string> UploadThumbnailAsync(Stream thumbnailStream, string videoBlobName, CancellationToken cancellationToken = default)
    {
        // Create thumbnail blob name based on video blob name
        var thumbnailBlobName = $"thumbnails/{Path.GetFileNameWithoutExtension(videoBlobName)}_thumb.jpg";

        try
        {
            var blobClient = await GetBlobClientAsync(thumbnailBlobName, cancellationToken);

            var uploadOptions = new BlobUploadOptions 
            { 
                HttpHeaders = new BlobHttpHeaders { ContentType = "image/jpeg" }
            };

            await blobClient.UploadAsync(
                thumbnailStream,
                uploadOptions,
                cancellationToken);

            _logger.LogInformation("Thumbnail uploaded successfully: {ThumbnailBlobName} for video: {VideoBlobName}", 
                thumbnailBlobName, videoBlobName);
            return thumbnailBlobName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading thumbnail for video: {VideoBlobName}", videoBlobName);
            throw;
        }
    }

    public async Task<string?> GetThumbnailUrlAsync(string thumbnailBlobName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(thumbnailBlobName))
        {
            return null;
        }

        try
        {
            var blobClient = GetBlobClient(thumbnailBlobName);

            // Check if blob exists
            if (!await blobClient.ExistsAsync(cancellationToken))
            {
                _logger.LogWarning("Thumbnail not found: {ThumbnailBlobName}", thumbnailBlobName);
                return null;
            }

            // Generate SAS token valid for 1 hour
            if (blobClient.CanGenerateSasUri)
            {
                // Account key-based SAS (local development)
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = "videos",
                    BlobName = thumbnailBlobName,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
                };

                sasBuilder.SetPermissions(BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                _logger.LogInformation("Generated account key SAS URL for thumbnail: {ThumbnailBlobName}", thumbnailBlobName);
                return sasUri.ToString();
            }
            else
            {
                // User delegation SAS (Azure production with managed identity)
                _logger.LogInformation("Generating user delegation SAS token for thumbnail: {ThumbnailBlobName}", thumbnailBlobName);
                
                var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5);
                var expiresOn = DateTimeOffset.UtcNow.AddHours(1);
                
                var userDelegationKey = await _blobServiceClient.GetUserDelegationKeyAsync(startsOn, expiresOn, cancellationToken);
                
                var sasBuilder = new BlobSasBuilder
                {
                    BlobContainerName = "videos",
                    BlobName = thumbnailBlobName,
                    Resource = "b",
                    StartsOn = startsOn,
                    ExpiresOn = expiresOn
                };
                
                sasBuilder.SetPermissions(BlobSasPermissions.Read);
                
                var blobUriBuilder = new BlobUriBuilder(blobClient.Uri)
                {
                    Sas = sasBuilder.ToSasQueryParameters(userDelegationKey.Value, _blobServiceClient.AccountName)
                };
                
                _logger.LogInformation("Generated user delegation SAS URL for thumbnail: {ThumbnailBlobName}", thumbnailBlobName);
                return blobUriBuilder.ToUri().ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thumbnail URL: {ThumbnailBlobName}", thumbnailBlobName);
            return null;
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
    string OwnerId,
    string? ThumbnailBlobName = null
);