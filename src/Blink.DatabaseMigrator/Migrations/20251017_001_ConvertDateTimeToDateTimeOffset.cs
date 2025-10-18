using FluentMigrator;

namespace Blink.DatabaseMigrator.Migrations;

[Migration(20251017_001)]
public sealed class ConvertDateTimeToDateTimeOffset : Migration
{
    public override void Up()
    {
        // Convert users table columns
        Alter.Table("users")
            .AlterColumn("created_at").AsDateTimeOffset().NotNullable()
            .AlterColumn("updated_at").AsDateTimeOffset().NotNullable();

        // Convert keycloak.processed_events table columns
        Alter.Table("processed_events").InSchema("keycloak")
            .AlterColumn("processed_at").AsDateTimeOffset().NotNullable();

        // Convert keycloak.event_checkpoints table columns
        Alter.Table("event_checkpoints").InSchema("keycloak")
            .AlterColumn("last_updated_at").AsDateTimeOffset().NotNullable();

        // Convert videos table columns
        Alter.Table("videos")
            .AlterColumn("video_date").AsDateTimeOffset().Nullable()
            .AlterColumn("uploaded_at").AsDateTimeOffset().NotNullable()
            .AlterColumn("updated_at").AsDateTimeOffset().NotNullable();
    }

    public override void Down()
    {
        // Revert users table columns
        Alter.Table("users")
            .AlterColumn("created_at").AsDateTime().NotNullable()
            .AlterColumn("updated_at").AsDateTime().NotNullable();

        // Revert keycloak.processed_events table columns
        Alter.Table("processed_events").InSchema("keycloak")
            .AlterColumn("processed_at").AsDateTime().NotNullable();

        // Revert keycloak.event_checkpoints table columns
        Alter.Table("event_checkpoints").InSchema("keycloak")
            .AlterColumn("last_updated_at").AsDateTime().NotNullable();

        // Revert videos table columns
        Alter.Table("videos")
            .AlterColumn("video_date").AsDateTime().Nullable()
            .AlterColumn("uploaded_at").AsDateTime().NotNullable()
            .AlterColumn("updated_at").AsDateTime().NotNullable();
    }
}
