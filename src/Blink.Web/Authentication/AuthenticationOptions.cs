namespace Blink.Web.Authentication;

public sealed record AuthenticationOptions
{
    public int RevalidationIntervalInSeconds { get; init; } = 900; // 15 min
}
