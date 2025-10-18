using Blink.Web.Components.Shared;
using Dapper;
using MediatR;
using Npgsql;

namespace Blink.Web.Features.People;

public sealed record GetPeopleQuery(string? SearchQuery = null) : IRequest<List<MentionTextarea.MentionItem>>;

internal sealed class GetPeopleQueryHandler : IRequestHandler<GetPeopleQuery, List<MentionTextarea.MentionItem>>
{
    private readonly NpgsqlDataSource _dataSource;

    public GetPeopleQueryHandler(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<MentionTextarea.MentionItem>> Handle(GetPeopleQuery request, CancellationToken cancellationToken)
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
        if (!string.IsNullOrWhiteSpace(request.SearchQuery))
        {
            sql += " WHERE name ILIKE @SearchQuery";
            parameters.Add("SearchQuery", $"%{request.SearchQuery}%");
        }

        sql += " ORDER BY name";

        var people = await connection.QueryAsync<MentionTextarea.MentionItem>(sql, parameters);
        return people.ToList();
    }
}

