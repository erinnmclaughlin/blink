using FluentMigrator;

namespace Blink.DatabaseMigrator.Migrations;

[Migration(20241012_001)]
public sealed class AddThumbnailToVideos : Migration
{
    public override void Up()
    {
        Alter.Table("videos")
            .AddColumn("thumbnail_blob_name").AsString(512).Nullable();
    }

    public override void Down()
    {
        Delete.Column("thumbnail_blob_name").FromTable("videos");
    }
}
