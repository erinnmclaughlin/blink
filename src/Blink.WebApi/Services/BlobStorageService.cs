using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace Blink.WebApi.Services;

public interface IBlobStorageService
{
    Task<string> UploadVideoAsync(Stream videoStream, string fileName, CancellationToken cancellationToken = default);
    Task<bool> DeleteVideoAsync(string blobName, CancellationToken cancellationToken = default);
    Task<Stream> DownloadVideoAsync(string blobName, CancellationToken cancellationToken = default);
}

public class BlobStorageService : IBlobStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        BlobServiceClient blobServiceClient, 
        ILogger<BlobStorageService> logger)
    {
        _blobServiceClient = blobServiceClient;
        _logger = logger;
    }

    public async Task<string> UploadVideoAsync(Stream videoStream, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("videos");
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

            // Generate a unique blob name
            var blobName = $"{Guid.NewGuid()}_{fileName}";
            var blobClient = containerClient.GetBlobClient(blobName);

            // Set content type based on file extension
            var contentType = GetContentType(fileName);
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

            await blobClient.UploadAsync(
                videoStream, 
                new BlobUploadOptions { HttpHeaders = blobHttpHeaders },
                cancellationToken);

            _logger.LogInformation("Video uploaded successfully: {BlobName}", blobName);
            return blobName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading video: {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteVideoAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("videos");
            var blobClient = containerClient.GetBlobClient(blobName);
            
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

    public async Task<Stream> DownloadVideoAsync(string blobName, CancellationToken cancellationToken = default)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient("videos");
            var blobClient = containerClient.GetBlobClient(blobName);
            
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

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".wmv" => "video/x-ms-wmv",
            ".flv" => "video/x-flv",
            ".webm" => "video/webm",
            ".mkv" => "video/x-matroska",
            _ => "application/octet-stream"
        };
    }
}

