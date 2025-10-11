using System.Net.Http.Json;

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

    public async Task<VideoUploadResponse> UploadVideoAsync(Stream videoStream, string fileName, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        using var streamContent = new StreamContent(videoStream);
        content.Add(streamContent, "video", fileName);

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
    string ContentType
);

public sealed record VideoUrlResponse(
    string Url
);
