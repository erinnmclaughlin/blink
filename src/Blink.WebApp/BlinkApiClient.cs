using Blink.VideosApi.Contracts.CompleteUpload;
using Blink.VideosApi.Contracts.Delete;
using Blink.VideosApi.Contracts.GetByBlobName;
using Blink.VideosApi.Contracts.GetUrl;
using Blink.VideosApi.Contracts.InitiateUpload;
using Blink.VideosApi.Contracts.List;
using Blink.VideosApi.Contracts.UpdateMetadata;
using Blink.VideosApi.Contracts.UpdateTitle;
using Blink.VideosApi.Contracts.Upload;

namespace Blink.WebApp;

public sealed class BlinkApiClient
{
    private readonly HttpClient _httpClient;

    public BlinkApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetTestMessage(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("test", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<string> GetAuthenticatedTestMessage(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("test-auth", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    public async Task<Dictionary<string, string[]>> GetClaims(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<Dictionary<string, string[]>>("claims", cancellationToken) ?? [];
    }

    /// <summary>
    /// Initiates a direct upload by requesting a SAS URL from the server
    /// </summary>
    public async Task<InitiateUploadResponse> InitiateUploadAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var request = new InitiateUploadRequest { FileName = fileName };
        var response = await _httpClient.PostAsJsonAsync("api/videos/initiate-upload", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<InitiateUploadResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize initiate upload response");
    }

    /// <summary>
    /// Notifies the server that a direct upload has been completed
    /// </summary>
    public async Task<CompleteUploadResponse> CompleteUploadAsync(
        string blobName,
        string fileName,
        string? title = null,
        string? description = null,
        DateOnly? videoDate = null,
        CancellationToken cancellationToken = default)
    {
        var request = new CompleteUploadRequest
        {
            BlobName = blobName,
            FileName = fileName,
            Title = title,
            Description = description,
            VideoDate = videoDate?.ToDateTime(TimeOnly.MinValue)
        };

        var response = await _httpClient.PostAsJsonAsync("api/videos/complete-upload", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<CompleteUploadResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Failed to deserialize complete upload response");
    }

    /// <summary>
    /// Legacy upload method - uploads video through the server (slower)
    /// </summary>
    public async Task<UploadedVideoInfo> UploadVideoAsync(
        Stream videoStream, 
        string fileName, 
        string? title = null,
        string? description = null,
        DateOnly? videoDate = null,
        CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(videoStream);
        content.Add(streamContent, "video", fileName);
        
        if (!string.IsNullOrWhiteSpace(title))
            content.Add(new StringContent(title), "title");
            
        if (!string.IsNullOrWhiteSpace(description))
            content.Add(new StringContent(description), "description");
            
        if (videoDate.HasValue)
            content.Add(new StringContent(videoDate.Value.ToString("yyyy-MM-dd")), "videoDate");

        var response = await _httpClient.PostAsync("api/videos/upload", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<UploadedVideoInfo>(cancellationToken) 
            ?? throw new InvalidOperationException("Failed to deserialize upload response");
    }

    public async Task<List<VideoSummaryDto>> GetVideosAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<VideoSummaryDto>>("api/videos", cancellationToken) ?? [];
    }

    public async Task<VideoDetailDto> GetVideoAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var encodedBlobName = Uri.EscapeDataString(blobName);
        var response = await _httpClient.GetFromJsonAsync<VideoDetailDto>($"api/videos/{encodedBlobName}", cancellationToken);
        return response ?? throw new InvalidOperationException("Video not found");
    }

    public async Task<string> GetVideoUrlAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var encodedBlobName = Uri.EscapeDataString(blobName);
        var response = await _httpClient.GetFromJsonAsync<VideoUrlResponse>($"api/videos/{encodedBlobName}/url", cancellationToken);
        return response?.Url ?? throw new InvalidOperationException("Failed to get video URL");
    }

    public async Task<VideoUrlResponse> GetVideoUrlWithThumbnailAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var encodedBlobName = Uri.EscapeDataString(blobName);
        var response = await _httpClient.GetFromJsonAsync<VideoUrlResponse>($"api/videos/{encodedBlobName}/url", cancellationToken);
        return response ?? throw new InvalidOperationException("Failed to get video URL");
    }

    public async Task<DeleteVideoResponse> DeleteVideoAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var encodedBlobName = Uri.EscapeDataString(blobName);
        var response = await _httpClient.DeleteAsync($"api/videos/{encodedBlobName}", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<DeleteVideoResponse>(cancellationToken) 
            ?? throw new InvalidOperationException("Failed to deserialize delete response");
    }

    public async Task<UpdateTitleResponse> UpdateVideoTitleAsync(string blobName, string title, CancellationToken cancellationToken = default)
    {
        var encodedBlobName = Uri.EscapeDataString(blobName);
        var response = await _httpClient.PutAsJsonAsync($"api/videos/{encodedBlobName}/title", new { Title = title }, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<UpdateTitleResponse>(cancellationToken) 
            ?? throw new InvalidOperationException("Failed to deserialize update title response");
    }

    public async Task<UpdateMetadataResponse> UpdateVideoMetadataAsync(
        string blobName, 
        string title, 
        string? description = null, 
        DateOnly? videoDate = null,
        CancellationToken cancellationToken = default)
    {
        var encodedBlobName = Uri.EscapeDataString(blobName);
        var response = await _httpClient.PutAsJsonAsync($"api/videos/{encodedBlobName}/metadata", new 
        { 
            Title = title,
            Description = description,
            VideoDate = videoDate?.ToString("yyyy-MM-dd")
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<UpdateMetadataResponse>(cancellationToken) 
            ?? throw new InvalidOperationException("Failed to deserialize update metadata response");
    }
}
