using MediatR;

namespace Blink.Web.Features.People;

public static class PeopleEndpoints
{
    public static void MapPeopleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/people")
            .RequireAuthorization()
            .WithTags("People");

        // Get all people (with optional search)
        group.MapGet("/", async (ISender sender, string? search = null) =>
        {
            var people = await sender.Send(new GetPeopleQuery(search));
            return Results.Ok(people);
        })
        .WithName("GetPeople");

        // Create a new person
        group.MapPost("/", async (ISender sender, CreatePersonRequest request) =>
        {
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

