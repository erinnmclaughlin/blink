using Blink.ApiService.Database;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.AddNpgsqlDbContext<BlinkDbContext>("blink-db");
builder.EnrichNpgsqlDbContext<BlinkDbContext>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<BlinkDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapGet("/users", async (BlinkDbContext db) => await db.Users.ToListAsync());

app.Run();
