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

    public async Task<string> GetClaims(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("claims", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
