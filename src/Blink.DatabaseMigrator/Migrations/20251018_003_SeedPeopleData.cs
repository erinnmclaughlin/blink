using FluentMigrator;

namespace Blink.DatabaseMigrator.Migrations;

[Migration(20251018_003)]
public sealed class SeedPeopleData : Migration
{
    public override void Up()
    {
        // Insert some sample people for development
        Insert.IntoTable("people").Row(new
        {
            id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
            name = "Erin McLaughlin",
            linked_user_id = (string?)null,
            created_by = "system",
            created_at = DateTimeOffset.UtcNow,
            updated_at = DateTimeOffset.UtcNow
        });

        Insert.IntoTable("people").Row(new
        {
            id = Guid.Parse("10000000-0000-0000-0000-000000000002"),
            name = "John Doe",
            linked_user_id = (string?)null,
            created_by = "system",
            created_at = DateTimeOffset.UtcNow,
            updated_at = DateTimeOffset.UtcNow
        });

        Insert.IntoTable("people").Row(new
        {
            id = Guid.Parse("10000000-0000-0000-0000-000000000003"),
            name = "Alex Martinez",
            linked_user_id = (string?)null,
            created_by = "system",
            created_at = DateTimeOffset.UtcNow,
            updated_at = DateTimeOffset.UtcNow
        });

        Insert.IntoTable("people").Row(new
        {
            id = Guid.Parse("10000000-0000-0000-0000-000000000004"),
            name = "Sarah Kim",
            linked_user_id = (string?)null,
            created_by = "system",
            created_at = DateTimeOffset.UtcNow,
            updated_at = DateTimeOffset.UtcNow
        });

        Insert.IntoTable("people").Row(new
        {
            id = Guid.Parse("10000000-0000-0000-0000-000000000005"),
            name = "Lisa Thompson",
            linked_user_id = (string?)null,
            created_by = "system",
            created_at = DateTimeOffset.UtcNow,
            updated_at = DateTimeOffset.UtcNow
        });
    }

    public override void Down()
    {
        Delete.FromTable("people").Row(new { id = Guid.Parse("10000000-0000-0000-0000-000000000001") });
        Delete.FromTable("people").Row(new { id = Guid.Parse("10000000-0000-0000-0000-000000000002") });
        Delete.FromTable("people").Row(new { id = Guid.Parse("10000000-0000-0000-0000-000000000003") });
        Delete.FromTable("people").Row(new { id = Guid.Parse("10000000-0000-0000-0000-000000000004") });
        Delete.FromTable("people").Row(new { id = Guid.Parse("10000000-0000-0000-0000-000000000005") });
    }
}

