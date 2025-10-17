using FluentMigrator;

namespace Blink.Web.Migrations;

[Migration(20241012_002)]
public sealed class AddAspectRatioToVideos : Migration
{
    public override void Up()
    {
        Alter.Table("videos")
            .AddColumn("width").AsInt32().Nullable()
            .AddColumn("height").AsInt32().Nullable();
    }

    public override void Down()
    {
        Delete.Column("width").FromTable("videos");
        Delete.Column("height").FromTable("videos");
    }
}

