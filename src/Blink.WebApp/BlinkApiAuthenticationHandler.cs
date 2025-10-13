using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Claims;

namespace Blink.WebApp;

/// <summary>
/// DelegatingHandler that adds the access token to outgoing requests to the Blink API
/// </summary>
public sealed class BlinkApiAuthenticationHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BlinkApiAuthenticationHandler> _logger;

    public BlinkApiAuthenticationHandler(IServiceProvider serviceProvider, ILogger<BlinkApiAuthenticationHandler> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get HttpContext from the service provider
        var httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext != null && httpContext.User.Identity?.IsAuthenticated == true)
        {
            _logger.LogDebug("User is authenticated, retrieving access token");
            
            // Get the current tokens
            var accessToken = await httpContext.GetTokenAsync("access_token");
            var refreshToken = await httpContext.GetTokenAsync("refresh_token");
            var expiresAt = await httpContext.GetTokenAsync("expires_at");

            _logger.LogDebug("Access token present: {HasToken}, Expires at: {ExpiresAt}", 
                !string.IsNullOrEmpty(accessToken), expiresAt);

            // Check if token is expired or about to expire (within 60 seconds)
            if (!string.IsNullOrEmpty(expiresAt) && 
                DateTime.TryParse(expiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expirationTime))
            {
                var timeUntilExpiry = expirationTime - DateTime.UtcNow;
                _logger.LogDebug("Token expires in {Minutes} minutes", timeUntilExpiry.TotalMinutes);
                
                if (expirationTime <= DateTime.UtcNow.AddSeconds(60))
                {
                    _logger.LogInformation("Token is expired or about to expire, attempting refresh");
                    // Token is expired or about to expire, refresh it
                    var newTokens = await RefreshTokensAsync(httpContext, refreshToken);
                    if (newTokens != null)
                    {
                        accessToken = newTokens.Value.accessToken;
                        _logger.LogInformation("Token refreshed successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Token refresh failed");
                    }
                }
            }

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                _logger.LogDebug("Authorization header added to request");
            }
            else
            {
                _logger.LogWarning("No access token available to add to request");
            }
        }
        else
        {
            _logger.LogDebug("User is not authenticated or HttpContext is null");
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<(string accessToken, string refreshToken, DateTime expiresAt)?> RefreshTokensAsync(
        HttpContext httpContext, 
        string? refreshToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("No refresh token available");
            return null;
        }

        try
        {
            // Get the OIDC options to retrieve the authority and other config
            var oidcOptions = _serviceProvider.GetRequiredService<IOptionsSnapshot<OpenIdConnectOptions>>();
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

            // Try to update the authentication properties with the new tokens
            // This may fail if the response has already started (e.g., in Blazor SSR with streaming)
            try
            {
                var authenticateResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (authenticateResult?.Properties != null && authenticateResult.Principal != null)
                {
                    authenticateResult.Properties.UpdateTokenValue("access_token", tokenResponse.AccessToken);
                    authenticateResult.Properties.UpdateTokenValue("refresh_token", tokenResponse.RefreshToken ?? refreshToken);
                    authenticateResult.Properties.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));

                    // Re-sign in to update the cookie with new tokens
                    // This will fail if response has already started
                    if (!httpContext.Response.HasStarted)
                    {
                        await httpContext.SignInAsync(
                            CookieAuthenticationDefaults.AuthenticationScheme,
                            authenticateResult.Principal,
                            authenticateResult.Properties);
                        _logger.LogInformation("Successfully updated stored tokens in cookie");
                    }
                    else
                    {
                        _logger.LogDebug("Cannot update cookie - response has already started. Token will be refreshed on next request.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update cookie with new tokens - response may have already started. Token will be used for this request only.");
            }

            return (tokenResponse.AccessToken, tokenResponse.RefreshToken ?? refreshToken, expiresAt);
        }
        catch (Exception ex)
        {
            // If refresh fails, return null and let the request proceed with the old token
            // The API will return 401, and the user can log in again
            _logger.LogError(ex, "Exception during token refresh");
            return null;
        }
    }

    private class TokenResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }
}

