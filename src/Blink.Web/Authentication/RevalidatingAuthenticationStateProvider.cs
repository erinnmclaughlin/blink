using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.Options;

namespace Blink.Web.Authentication;

// This is a server-side AuthenticationStateProvider that revalidates the security stamp for the connected user
// every 15 minutes an interactive circuit is connected.
internal sealed class RevalidatingAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IDateProvider _dateProvider;
    private readonly IOptionsMonitor<AuthenticationOptions> _optionsMonitor;
    private readonly IServiceScopeFactory _scopeFactory;
    
    protected override TimeSpan RevalidationInterval => TimeSpan.FromSeconds(_optionsMonitor.CurrentValue.RevalidationIntervalInSeconds);
    
    public RevalidatingAuthenticationStateProvider(
        IDateProvider dateProvider,
        ILoggerFactory loggerFactory,
        IOptionsMonitor<AuthenticationOptions> optionsMonitor, 
        IServiceScopeFactory scopeFactory
    ) : base(loggerFactory)
    {
        _dateProvider = dateProvider;
        _optionsMonitor = optionsMonitor;
        _scopeFactory = scopeFactory;
    }

    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        // If the current principal is not authenticated, consider it invalid
        if (authenticationState.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        // If there's no current HttpContext (can occur outside a request), skip invalidation
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
        
        return !TryParseDateTimeOffset(expiresAtValue, out var expiresAt) || expiresAt > _dateProvider.UtcNow;
    }

    private static bool TryParseDateTimeOffset(string? value, out DateTimeOffset dateTimeOffset)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dateTimeOffset);
    }
}
