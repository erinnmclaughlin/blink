using FluentMigrator;

namespace Blink.Web.Migrations;

[Migration(20250928_002)]
public class AddKeycloakEventListenerTables : Migration
{
    public override void Up()
    {
        Create.Schema("keycloak");

        Create.Table("processed_events").InSchema("keycloak")
            .WithColumn("id").AsInt64().PrimaryKey().Identity()
            .WithColumn("event_id").AsString(256).NotNullable().Unique()
            .WithColumn("processed_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Table("event_checkpoints").InSchema("keycloak")
            .WithColumn("id").AsInt64().PrimaryKey()
            .WithColumn("last_updated_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);
    }

    public override void Down()
    {
        Delete.Table("processed_events").InSchema("keycloak");
        Delete.Table("event_checkpoints").InSchema("keycloak");
        Delete.Schema("keycloak");
    }

}
