using Dapper;
using MediatR;
using Npgsql;

namespace Blink.Web.Mentions.Requests;

public static class GetMentionablePeople
{
    public sealed record Query(
        string? SearchText = null, 
        int Skip = 0, 
        int Take = 50) : IRequest<List<PersonListItem>>;
    
    public sealed record PersonListItem
    {
        public string Id { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
    }

    internal sealed class QueryHandler : IRequestHandler<Query, List<PersonListItem>>
    {
        private readonly NpgsqlDataSource _dataSource;

        public QueryHandler(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public async Task<List<PersonListItem>> Handle(Query request, CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

            var sql = """
                      SELECT 
                          id::text as Id,
                          name as Name,
                          CASE 
                              WHEN linked_user_id IS NOT NULL THEN 'Linked User'
                              ELSE 'Person'
                          END as Subtitle
                      FROM people
                      """;

            var parameters = new DynamicParameters();

            // Add search filter if provided
            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                sql += " WHERE name ILIKE @SearchQuery";
                parameters.Add("SearchQuery", $"%{request.SearchText}%");
            }

            sql += " ORDER BY name";
        
            // Add pagination
            sql += " LIMIT @Take OFFSET @Skip";
            parameters.Add("Take", request.Take);
            parameters.Add("Skip", request.Skip);

            var people = await connection.QueryAsync<PersonListItem>(sql, parameters);
            return people.ToList();
        }
    }
}
