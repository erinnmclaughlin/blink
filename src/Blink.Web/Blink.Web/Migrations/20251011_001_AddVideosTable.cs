using FluentMigrator;

namespace Blink.Web.Migrations;

[Migration(20241011_001)]
public sealed class AddVideosTable : Migration
{
    public override void Up()
    {
        Create.Table("videos")
            .WithColumn("id").AsGuid().NotNullable().PrimaryKey()
            .WithColumn("blob_name").AsString(512).NotNullable().Unique()
            .WithColumn("title").AsString(512).NotNullable()
            .WithColumn("description").AsString(2000).Nullable()
            .WithColumn("video_date").AsDateTime().Nullable()
            .WithColumn("file_name").AsString(512).NotNullable()
            .WithColumn("content_type").AsString(256).NotNullable()
            .WithColumn("size_in_bytes").AsInt64().NotNullable()
            .WithColumn("owner_id").AsString(256).NotNullable()
            .WithColumn("uploaded_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
            .WithColumn("updated_at").AsDateTime().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime);

        Create.Index("ix_videos_owner_id")
            .OnTable("videos")
            .OnColumn("owner_id");

        Create.Index("ix_videos_uploaded_at")
            .OnTable("videos")
            .OnColumn("uploaded_at");
    }

    public override void Down()
    {
        Delete.Table("videos");
    }
}
