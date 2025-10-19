using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

namespace Blink.Web.Authentication;

// This is a server-side AuthenticationStateProvider that revalidates the security stamp for the connected user
// every 15 minutes an interactive circuit is connected.
internal sealed class RevalidatingAuthenticationStateProvider(
    ILoggerFactory loggerFactory,
    IServiceScopeFactory scopeFactory
) : RevalidatingServerAuthenticationStateProvider(loggerFactory)
{
    protected override TimeSpan RevalidationInterval => TimeSpan.FromMinutes(15);

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        // If the current principal is not authenticated, consider it invalid
        if (authenticationState.User?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        // If there's no current HttpContext (can occur outside of a request), skip invalidation
        if (httpContext is null)
        {
            return true;
        }

        // Re-authenticate using the cookie scheme to get fresh authentication properties (including expiry and tokens)
        var authenticateResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (!authenticateResult.Succeeded || authenticateResult.Principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        // Check the cookie's expiry, if present
        var cookieExpiresUtc = authenticateResult.Properties?.ExpiresUtc;
        if (cookieExpiresUtc.HasValue && cookieExpiresUtc.Value <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        // If tokens were saved (SaveTokens = true), check the access token expiry ("expires_at")
        var tokens = authenticateResult.Properties?.GetTokens();
        var expiresAtValue = tokens?.FirstOrDefault(t => string.Equals(t.Name, "expires_at", StringComparison.OrdinalIgnoreCase))?.Value;
        if (!string.IsNullOrEmpty(expiresAtValue))
        {
            if (DateTimeOffset.TryParse(expiresAtValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var accessTokenExpiresAt))
            {
                if (accessTokenExpiresAt <= DateTimeOffset.UtcNow)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
