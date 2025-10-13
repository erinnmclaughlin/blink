using System.Net.Http.Json;

namespace Blink.BlazorSSR;

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

    public async Task<VideoUploadResponse> UploadVideoAsync(
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
        
        return await response.Content.ReadFromJsonAsync<VideoUploadResponse>(cancellationToken) 
            ?? throw new InvalidOperationException("Failed to deserialize upload response");
    }

    public async Task<List<VideoInfo>> GetVideosAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<VideoInfo>>("api/videos", cancellationToken) ?? [];
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

    public async Task<VideoDeleteResponse> DeleteVideoAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var encodedBlobName = Uri.EscapeDataString(blobName);
        var response = await _httpClient.DeleteAsync($"api/videos/{encodedBlobName}", cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<VideoDeleteResponse>(cancellationToken) 
            ?? throw new InvalidOperationException("Failed to deserialize delete response");
    }

    public async Task<VideoTitleUpdateResponse> UpdateVideoTitleAsync(string blobName, string title, CancellationToken cancellationToken = default)
    {
        var encodedBlobName = Uri.EscapeDataString(blobName);
        var response = await _httpClient.PutAsJsonAsync($"api/videos/{encodedBlobName}/title", new { Title = title }, cancellationToken);
        response.EnsureSuccessStatusCode();
        
        return await response.Content.ReadFromJsonAsync<VideoTitleUpdateResponse>(cancellationToken) 
            ?? throw new InvalidOperationException("Failed to deserialize update title response");
    }

    public async Task<VideoMetadataUpdateResponse> UpdateVideoMetadataAsync(
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
        
        return await response.Content.ReadFromJsonAsync<VideoMetadataUpdateResponse>(cancellationToken) 
            ?? throw new InvalidOperationException("Failed to deserialize update metadata response");
    }
}

public sealed record VideoUploadResponse(
    string Message,
    string BlobName,
    string FileName,
    long FileSize
);

public sealed record VideoInfo(
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

public sealed record VideoUrlResponse(
    string Url,
    string? ThumbnailUrl = null
);

public sealed record VideoDeleteResponse(
    bool Success,
    string Message,
    string BlobName
);

public sealed record VideoTitleUpdateResponse(
    bool Success,
    string Message,
    string BlobName,
    string Title
);

public sealed record VideoMetadataUpdateResponse(
    bool Success,
    string Message,
    string BlobName,
    string Title,
    string? Description,
    DateTime? VideoDate
);

