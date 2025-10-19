using Blink.Web.Authentication;
using Dapper;
using MediatR;
using Npgsql;

namespace Blink.Web.Mentions.Features;

public sealed record CreatePersonCommand : IRequest<Guid>
{
    public required string Name { get; init; }
    public string? LinkedUserId { get; init; }
}

internal sealed class CreatePersonCommandHandler : IRequestHandler<CreatePersonCommand, Guid>
{
    private readonly ICurrentUser _currentUser;
    private readonly NpgsqlDataSource _dataSource;
    private readonly IDateProvider _dateProvider;
    private readonly IGuidGenerator _guidGenerator;

    public CreatePersonCommandHandler(
        ICurrentUser currentUser,
        NpgsqlDataSource dataSource, 
        IDateProvider dateProvider,
        IGuidGenerator guidGenerator)
    {
        _currentUser = currentUser;
        _dataSource = dataSource;
        _dateProvider = dateProvider;
        _guidGenerator = guidGenerator;
    }

    public async Task<Guid> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var personId = _guidGenerator.NewGuid();
        var now = _dateProvider.UtcNow;

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
            created_by = _currentUser.Id,
            created_at = now,
            updated_at = now
        });

        return personId;
    }
}
