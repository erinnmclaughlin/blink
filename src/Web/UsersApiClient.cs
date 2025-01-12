// ReSharper disable ClassNeverInstantiated.Global

using System.Net.Http.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Blink.Web;

public sealed class UsersApiClient(HttpClient httpClient)
{
    public async Task<UserClaim[]> ListCurrentUserClaimsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await httpClient.GetAsync("/users/me", cancellationToken);
            return await response.Content.ReadFromJsonAsync<UserClaim[]>(cancellationToken) ?? [];
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return [];
        }
    }
    
    public async Task<List<UserSummary>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<UserSummary>>("/users", cancellationToken) ?? [];
        }
        catch (AccessTokenNotAvailableException ex)
        {
            ex.Redirect();
            return [];
        }
    }
}

public sealed record UserClaim(string Type, string Value);

public sealed record UserSummary(Guid Id, string EmailAddress, string? FirstName, string? LastName, string? DisplayName);
