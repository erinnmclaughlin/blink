using FluentMigrator;

namespace Blink.Web.Migrations;

[Migration(20250928_001)]
public sealed class AddUserTable : Migration
{
    public override void Up()
    {
        Create.Table("users")
            .WithColumn("id").AsString(256).NotNullable().PrimaryKey()
            .WithColumn("username").AsString(256).NotNullable().Unique()
            .WithColumn("email").AsString(256).NotNullable().Unique()
            .WithColumn("first_name").AsString(256).NotNullable()
            .WithColumn("last_name").AsString(256).NotNullable()
            .WithColumn("created_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);
    }

    public override void Down()
    {
        Delete.Table("users");
    }
}
