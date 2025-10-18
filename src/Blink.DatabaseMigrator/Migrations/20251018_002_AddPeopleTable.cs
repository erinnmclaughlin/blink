using FluentMigrator;

namespace Blink.DatabaseMigrator.Migrations;

[Migration(20251018_002)]
public sealed class AddPeopleTable : Migration
{
    public override void Up()
    {
        Create.Table("people")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("name").AsString(200).NotNullable()
            .WithColumn("linked_user_id").AsString(256).Nullable()
            .WithColumn("created_by").AsString(256).NotNullable()
            .WithColumn("created_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Index("ix_people_name")
            .OnTable("people")
            .OnColumn("name");

        Create.Index("ix_people_linked_user_id")
            .OnTable("people")
            .OnColumn("linked_user_id");
            
        Create.Index("ix_people_created_by")
            .OnTable("people")
            .OnColumn("created_by");
    }

    public override void Down()
    {
        Delete.Table("people");
    }
}
