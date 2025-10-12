using System.Security.Claims;

namespace Blink.WebApi;

public interface ICurrentUser
{
    string UserId { get; }
}

public sealed class CurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string UserId { get; } = GetCurrentUserId(httpContextAccessor);

    private static string GetCurrentUserId(IHttpContextAccessor httpContextAccessor)
    {
        return httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value
            ?? string.Empty;
    }
}