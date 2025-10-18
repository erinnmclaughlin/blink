using FluentMigrator;

namespace Blink.DatabaseMigrator.Migrations;

[Migration(20251018_004)]
public sealed class AddTrigramIndexToPeopleName : Migration
{
    public override void Up()
    {
        // Enable pg_trgm extension for trigram-based text search
        Execute.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        
        // Drop the existing BTREE index on name
        Delete.Index("ix_people_name").OnTable("people");
        
        // Create a GIN trigram index for efficient ILIKE '%pattern%' queries
        Execute.Sql("CREATE INDEX ix_people_name_trgm ON people USING gin (name gin_trgm_ops);");
    }

    public override void Down()
    {
        // Drop the trigram index
        Execute.Sql("DROP INDEX IF EXISTS ix_people_name_trgm;");
        
        // Recreate the standard BTREE index
        Create.Index("ix_people_name")
            .OnTable("people")
            .OnColumn("name");
        
        // Note: We don't drop the pg_trgm extension in case other tables use it
    }
}
