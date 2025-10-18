using Dapper;
using MediatR;
using Npgsql;
using System.Security.Claims;

namespace Blink.Web.Features.People;

public sealed record CreatePersonCommand : IRequest<Guid>
{
    public required string Name { get; init; }
    public string? LinkedUserId { get; init; }
}

internal sealed class CreatePersonCommandHandler : IRequestHandler<CreatePersonCommand, Guid>
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreatePersonCommandHandler(NpgsqlDataSource dataSource, IHttpContextAccessor httpContextAccessor)
    {
        _dataSource = dataSource;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<Guid> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var personId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var currentUserId = GetCurrentUserId();

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        const string sql = """
            INSERT INTO people (
                id, name, linked_user_id, created_by, created_at, updated_at
            ) VALUES (
                @id, @name, @linked_user_id, @created_by, @created_at, @updated_at
            )
            """;

        await connection.ExecuteAsync(sql, new
        {
            id = personId,
            name = request.Name,
            linked_user_id = request.LinkedUserId,
            created_by = currentUserId,
            created_at = now,
            updated_at = now
        });

        return personId;
    }

    private string GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                         ?? user.FindFirst("sub")
                         ?? throw new InvalidOperationException("User ID not found in claims");

        return userIdClaim.Value;
    }
}

