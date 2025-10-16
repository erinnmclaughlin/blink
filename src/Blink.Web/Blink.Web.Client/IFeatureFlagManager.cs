using System.Net.Http.Json;

namespace Blink.Web.Client;

public interface IFeatureFlagManager
{
    Task<bool> IsEnabledAsync(string featureFlagName);
}

public sealed class WasmFeatureFlagManager : IFeatureFlagManager
{
    private readonly HttpClient _httpClient;

    public WasmFeatureFlagManager(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<bool> IsEnabledAsync(string featureFlagName)
    {
        return await _httpClient.GetFromJsonAsync<bool>($"api/features/{featureFlagName}");
    }
}