using Microsoft.AspNetCore.Components;
using System.Security.Claims;

namespace Blink.Web.Client.Components;

public sealed partial class AppAuthorizeView
{
    [Parameter]
    public RenderFragment<CurrentUser>? ChildContent { get; set; }

    [Parameter]
    public RenderFragment<CurrentUser>? Authorized { get; set; }

    [Parameter]
    public RenderFragment? NotAuthorized { get; set; }

    protected override void OnParametersSet()
    {
        if (ChildContent is not null && (Authorized is not null || NotAuthorized is not null))
        {
            throw new InvalidOperationException($"Cannot set both {nameof(ChildContent)} and {nameof(Authorized)}/{nameof(NotAuthorized)}.");
        }
    }
}

public sealed record CurrentUser
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string UserName { get; init; }
    public required string Email { get; init; }

    public static CurrentUser FromClaims(IList<Claim> claims)
    {
        var firstName = claims.FindFirstValue(ClaimTypes.GivenName);
        var lastName = claims.FindFirstValue(ClaimTypes.Surname);
        var displayName = claims.FindFirstValueOrDefault(ClaimTypes.Name, "name") ?? $"{firstName} {lastName}";

        return new CurrentUser
        {
            Id = claims.FindFirstValue(ClaimTypes.NameIdentifier),
            DisplayName = displayName,
            FirstName = firstName,
            LastName = lastName,
            UserName = claims.FindFirstValue("preferred_username"),
            Email = claims.FindFirstValue(ClaimTypes.Email, "emails")
        };
    }
}

file static class Extensions
{
    public static string? FindFirstValueOrDefault(this IList<Claim> claims, params IEnumerable<string> claimTypes)
    {
        foreach (var claimType in claimTypes)
        {
            var value = claims.FirstOrDefault(c => c.Type == claimType)?.Value;

            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return null;
    }

    public static string FindFirstValue(this IList<Claim> claims, params IEnumerable<string> claimTypes)
    {
        return claims.FindFirstValueOrDefault(claimTypes) ?? string.Empty;
    }
}