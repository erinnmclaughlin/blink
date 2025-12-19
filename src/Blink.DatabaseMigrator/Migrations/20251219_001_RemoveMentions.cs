using FluentMigrator;

namespace Blink.DatabaseMigrator.Migrations;

[Migration(20251219_001)]
public sealed class RemoveMentions : Migration
{
    public override void Up()
    {
        Delete.Column("description_mentions").FromTable("videos");
    }

    public override void Down()
    {
        Alter.Table("videos")
            .AddColumn("description_mentions").AsCustom("jsonb").Nullable();
    }
}
