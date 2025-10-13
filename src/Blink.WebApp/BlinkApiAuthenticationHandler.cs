using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;

namespace Blink.WebApp;

/// <summary>
/// DelegatingHandler that adds the access token to outgoing requests to the Blink API
/// </summary>
public class BlinkApiAuthenticationHandler : DelegatingHandler
{
    private readonly IServiceProvider _serviceProvider;

    public BlinkApiAuthenticationHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Get HttpContext from the service provider
        var httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext != null)
        {
            // Get the access token from the authentication result
            var accessToken = await httpContext.GetTokenAsync("access_token");

            if (!string.IsNullOrEmpty(accessToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

