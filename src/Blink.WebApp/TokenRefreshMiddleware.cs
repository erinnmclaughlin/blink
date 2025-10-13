using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json.Serialization;

namespace Blink.WebApp;

/// <summary>
/// Middleware that checks and refreshes access tokens before the response starts.
/// This allows us to update cookies even in scenarios with streaming rendering.
/// </summary>
public class TokenRefreshMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenRefreshMiddleware> _logger;

    public TokenRefreshMiddleware(RequestDelegate next, ILogger<TokenRefreshMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only process if user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var accessToken = await context.GetTokenAsync("access_token");
            var refreshToken = await context.GetTokenAsync("refresh_token");
            var expiresAt = await context.GetTokenAsync("expires_at");

            // Check if token is expired or about to expire (within 60 seconds)
            if (!string.IsNullOrEmpty(expiresAt) &&
                !string.IsNullOrEmpty(refreshToken) &&
                DateTime.TryParse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expirationTime))
            {
                if (expirationTime <= DateTime.UtcNow.AddSeconds(60))
                {
                    _logger.LogInformation("Token is expired or about to expire, refreshing proactively");

                    var newTokens = await RefreshTokensAsync(context, refreshToken);
                    if (newTokens != null)
                    {
                        // Update the authentication properties with the new tokens
                        var authenticateResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        if (authenticateResult?.Properties != null && authenticateResult.Principal != null)
                        {
                            authenticateResult.Properties.UpdateTokenValue("access_token", newTokens.Value.accessToken);
                            authenticateResult.Properties.UpdateTokenValue("refresh_token", newTokens.Value.refreshToken);
                            authenticateResult.Properties.UpdateTokenValue("expires_at", newTokens.Value.expiresAt.ToString("o", CultureInfo.InvariantCulture));

                            // Re-sign in to update the cookie with new tokens
                            await context.SignInAsync(
                                CookieAuthenticationDefaults.AuthenticationScheme,
                                authenticateResult.Principal,
                                authenticateResult.Properties);

                            _logger.LogInformation("Token refreshed successfully and cookie updated");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Token refresh failed in middleware");
                    }
                }
            }
        }

        await _next(context);
    }

    private async Task<(string accessToken, string refreshToken, DateTime expiresAt)?> RefreshTokensAsync( HttpContext httpContext, string refreshToken)
    {
        try
        {
            var serviceProvider = httpContext.RequestServices;

            // Get the OIDC options to retrieve the authority and other config
            var oidcOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<OpenIdConnectOptions>>();
            var options = oidcOptions.Get(OpenIdConnectDefaults.AuthenticationScheme);

            var clientId = options.ClientId ?? "blink-webapp";

            // Get the configuration - this will trigger discovery if not already loaded
            if (options.ConfigurationManager == null)
            {
                _logger.LogError("OIDC ConfigurationManager is null");
                return null;
            }

            var oidcConfiguration = await options.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
            var tokenEndpoint = oidcConfiguration?.TokenEndpoint;

            if (string.IsNullOrEmpty(tokenEndpoint))
            {
                _logger.LogError("Token endpoint not found in OIDC configuration");
                return null;
            }

            _logger.LogDebug("Refreshing token using endpoint: {TokenEndpoint}", tokenEndpoint);

            using var httpClient = new HttpClient();
            var tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken,
                    ["client_id"] = clientId
                })
            };

            var response = await httpClient.SendAsync(tokenRequest);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Token refresh failed with status {StatusCode}: {Error}",
                    response.StatusCode, errorContent);
                return null;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>();
            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogError("Token refresh returned empty or invalid response");
                return null;
            }

            // Calculate expiration time
            var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

            return (tokenResponse.AccessToken, tokenResponse.RefreshToken ?? refreshToken, expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during token refresh in middleware");
            return null;
        }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}
