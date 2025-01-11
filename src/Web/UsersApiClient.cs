namespace Blink.Web;

public sealed class UsersApiClient(HttpClient httpClient)
{
    public async Task<List<UserSummary>> ListUsersAsync(CancellationToken cancellationToken = default)
    {
        return await httpClient.GetFromJsonAsync<List<UserSummary>>("/users", cancellationToken) ?? [];
    }
}

public sealed record UserSummary(Guid Id, string EmailAddress, string? FirstName, string? LastName, string? DisplayName);
