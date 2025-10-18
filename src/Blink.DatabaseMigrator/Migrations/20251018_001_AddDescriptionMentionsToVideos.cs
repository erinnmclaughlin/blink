using FluentMigrator;

namespace Blink.DatabaseMigrator.Migrations;

[Migration(20251018_001)]
public sealed class AddDescriptionMentionsToVideos : Migration
{
    public override void Up()
    {
        Alter.Table("videos")
            .AddColumn("description_mentions").AsCustom("jsonb").Nullable();
    }

    public override void Down()
    {
        Delete.Column("description_mentions").FromTable("videos");
    }
}

