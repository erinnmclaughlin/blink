using Dapper;
using MediatR;
using Npgsql;

namespace Blink.Web.Users.Features;

public static class GetUsers
{
    public sealed record Query : IRequest<List<User>>;

    public sealed record User
    {
        public required string Id { get; init; }
        public required string UserName { get; init; }
        public required string EmailAddress { get; init; }
    }

    internal sealed class QueryHandler : IRequestHandler<Query, List<User>>
    {
        private readonly NpgsqlDataSource _dataSource;

        public QueryHandler(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<List<User>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var connection = _dataSource.CreateConnection();
            var sql = """
                      SELECT 
                          id as Id,
                          username as UserName,
                          email as EmailAddress
                      FROM users
                      ORDER BY username
                      """;
            
            var users = await connection.QueryAsync<User>(sql);
            return users.ToList();
        }
    }
}
