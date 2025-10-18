using MediatR;

namespace Blink.Web.Features.People;

public static class PeopleEndpoints
{
    public static void MapPeopleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/people")
            .RequireAuthorization()
            .WithTags("People");

        // Get all people (with optional search and pagination)
        group.MapGet("/", async (ISender sender, string? search = null, int skip = 0, int take = 50) =>
        {
            // Enforce reasonable limits
            if (skip < 0) skip = 0;
            if (take < 1) take = 1;
            if (take > 100) take = 100; // Cap maximum page size
            
            var people = await sender.Send(new GetPeopleQuery(search, skip, take));
            return Results.Ok(people);
        })
        .WithName("GetPeople");

        // Create a new person
        group.MapPost("/", async (ISender sender, CreatePersonRequest request) =>
        {
            // Validate Name
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest("Name is required and cannot be empty");
            
            if (request.Name.Length > 200)
                return Results.BadRequest("Name must be <= 200 characters");
            
            // Validate LinkedUserId
            if (request.LinkedUserId?.Length > 256)
                return Results.BadRequest("LinkedUserId must be <= 256 characters");

            var command = new CreatePersonCommand
            {
                Name = request.Name,
                LinkedUserId = request.LinkedUserId
            };

            var personId = await sender.Send(command);
            return Results.Created($"/api/people/{personId}", new { id = personId });
        })
        .WithName("CreatePerson");
    }
}

public sealed record CreatePersonRequest
{
    public required string Name { get; init; }
    public string? LinkedUserId { get; init; }
}

