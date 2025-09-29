using Dapper;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Blink.WebApi.Keycloak;

public class KeycloakEventPoller : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonOptions _jsonOptions;
    private readonly ILogger<KeycloakEventPoller> _logger;
    private readonly KeycloakOptions _opt;
    private readonly IServiceProvider _serviceProvider;

    public KeycloakEventPoller(IHttpClientFactory httpClientFactory, IOptions<JsonOptions> jsonOptions, ILogger<KeycloakEventPoller> logger, IOptions<KeycloakOptions> options, IServiceProvider serviceProvider)
    {
        _httpClientFactory = httpClientFactory;
        _jsonOptions = jsonOptions.Value;
        _logger = logger;
        _opt = options.Value;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try { await PollOnce(ct); }
            catch (Exception ex) { _logger.LogError(ex, "Keycloak poll failed"); }
            await Task.Delay(TimeSpan.FromSeconds(_opt.PollIntervalSeconds), ct);
        }
    }

    private async Task PollOnce(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();

        await using var connection = await db.OpenConnectionAsync(ct);

        var token = await GetAdminTokenAsync(ct);
        var http = _httpClientFactory.CreateClient("keycloak");
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var first = 0;
        var any = false;
        var newestTimestamp = await GetLastUpdatedDate(connection);

        while (true)
        {
            var newestTimestampEpoch = new DateTimeOffset(newestTimestamp).ToUnixTimeMilliseconds().ToString();
            var url =
                $"{_opt.BaseUrl}/admin/realms/blink/admin-events" +
                $"?dateFrom={newestTimestampEpoch}" +
                $"&operationTypes=CREATE&resourceTypes=USER&first={first}&max={_opt.PageSize}";

            using var resp = await http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            
            var json = await resp.Content.ReadAsStringAsync(ct);
            var events = JsonSerializer.Deserialize<List<AdminEvent>>(json, _jsonOptions.SerializerOptions) ?? new();

            if (events.Count == 0) break;

            foreach (var e in events)
            {
                // Process only CREATE on USER (defensive check)
                if (!string.Equals(e.ResourceType, "USER", StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(e.OperationType, "CREATE", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Dedup
                if (await IsProcessed(connection, e.Id))
                    continue;

                // representation usually contains the full user JSON
                KeycloakUser? user = null;
                if (!string.IsNullOrWhiteSpace(e.Representation))
                {
                    user = JsonSerializer.Deserialize<KeycloakUser>(e.Representation, _jsonOptions.SerializerOptions);
                }

                if (user != null)
                {
                    user.Id ??= e.ResourcePath?.Replace("users/", "");
                    await UpsertUserAsync(connection, user, ct);
                }
                else
                {
                    // Fallback: fetch the user by id from resourcePath "users/{id}"
                    var id = e.ResourcePath?.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                    if (!string.IsNullOrEmpty(id))
                    {
                        var u = await FetchUserByIdAsync(http, id, ct);
                        if (u != null) 
                            await UpsertUserAsync(connection, u, ct);
                    }
                }

                // TODO: Save event to processed events table

                // Track newest timestamp seen
                var eventTime = new DateTimeOffset(e.Time, TimeSpan.Zero);
                if (eventTime > newestTimestamp)
                    newestTimestamp = eventTime.UtcDateTime;

                any = true;
            }

            if (events.Count < _opt.PageSize) break; // no more pages
            first += _opt.PageSize;
        }

        if (any)
        {
            await UpdateLastUpdatedDate(connection, newestTimestamp);
        }
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken ct)
    {
        var http = _httpClientFactory.CreateClient("keycloak");
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = _opt.ClientId,
            ["client_secret"] = _opt.ClientSecret
        };

        using var resp = await http.PostAsync(
            $"{_opt.BaseUrl}/realms/blink/protocol/openid-connect/token",
            new FormUrlEncodedContent(form), ct);

        var payload = await resp.Content.ReadAsStringAsync(ct);

        resp.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<JsonElement>(payload, _jsonOptions.SerializerOptions).GetProperty("access_token").GetString()!;
    }

    private static async Task<DateTime> GetLastUpdatedDate(NpgsqlConnection connection)
    {
        var date = await connection.QueryFirstOrDefaultAsync<DateTime?>("select last_updated_at from keycloak.event_checkpoints where id = 1");
        return date ?? DateTime.UtcNow.AddYears(-1);
    }

    private static async Task UpdateLastUpdatedDate(NpgsqlConnection connection, DateTime date)
    {
        await connection.ExecuteAsync(@"
            insert into keycloak.event_checkpoints (id, last_updated_at)
            values (1, @Date)
            on conflict (id) do update set last_updated_at = @Date
        ", new { Date = date });
    }

    private static async Task<bool> IsProcessed(NpgsqlConnection connection, string eventId)
    {
        return await connection.QueryFirstOrDefaultAsync<bool>("select exists(select 1 from keycloak.processed_events where event_id = @EventId)", new { EventId = eventId });
    }

    private static async Task UpsertUserAsync(NpgsqlConnection connection, KeycloakUser kc, CancellationToken ct)
    {
        await connection.ExecuteAsync(
            """
            insert into users (id, username, email, first_name, last_name)
            values (@Id, @Username, @Email, @FirstName, @LastName)
            on conflict (id) do update set
                username = excluded.username,
                email = excluded.email,
                first_name = excluded.first_name,
                last_name = excluded.last_name,
                updated_at = current_timestamp
            """,
            new { kc.Id, kc.Username, kc.Email, kc.FirstName, kc.LastName });
    }

    private async Task<KeycloakUser?> FetchUserByIdAsync(HttpClient http, string id, CancellationToken ct)
    {
        using var resp = await http.GetAsync($"{_opt.BaseUrl}/admin/realms/blink/users/{id}", ct);
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<KeycloakUser>(_jsonOptions.SerializerOptions, ct);
    }

    public sealed class AdminEvent
    {
        public string Id { get; set; } = default!;
        public string? OperationType { get; set; }          // "CREATE"
        public string? ResourceType { get; set; }           // "USER"
        public string? ResourcePath { get; set; }           // "users/{id}"
        public string? Representation { get; set; }
        public long Time { get; set; }
    }

    public sealed class KeycloakUser
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Email { get; set; }
        public bool? EmailVerified { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public Dictionary<string, string[]?>? Attributes { get; set; }
    }
}

public sealed record KeycloakOptions
{
    public string BaseUrl { get; set; } = "http+https://keycloak";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public int PageSize { get; set; } = 50;
    public int PollIntervalSeconds { get; set; } = 30;
}