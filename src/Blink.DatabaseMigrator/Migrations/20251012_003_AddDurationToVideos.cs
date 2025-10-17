using FluentMigrator;

namespace Blink.DatabaseMigrator.Migrations;

[Migration(20241012_003)]
public sealed class AddDurationToVideos : Migration
{
    public override void Up()
    {
        Alter.Table("videos")
            .AddColumn("duration_in_seconds").AsDouble().Nullable();
    }

    public override void Down()
    {
        Delete.Column("duration_in_seconds").FromTable("videos");
    }
}

