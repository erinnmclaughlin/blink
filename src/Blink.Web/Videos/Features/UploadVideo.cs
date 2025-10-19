using System.Text.Json;
using Blink.Messaging;
using Blink.Storage;
using Blink.Web.Authentication;
using Blink.Web.Mentions;
using Dapper;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using Npgsql;

namespace Blink.Web.Videos.Features;

public static class UploadVideo
{
    public const long MaxFileSize = 2000 * 1024 * 1024; // 2GB

    public sealed record Command : IRequest<Guid>
    {
        public required IBrowserFile VideoFile { get; init; }

        public string Title { get; init; } = "[No Title]";
        public string? Description { get; init; }
        public List<MentionMetadata>? DescriptionMentions { get; init; }
        public DateTime? VideoDate { get; init; }
    }

    public sealed class CommandHandler : IRequestHandler<Command, Guid>
    {
        private readonly ICurrentUser _currentUser;
        private readonly NpgsqlDataSource _dataSource;
        private readonly IDateProvider _dateProvider;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IVideoStorageClient _videoStorage;

        public CommandHandler(
            ICurrentUser currentUser,
            NpgsqlDataSource dataSource, 
            IDateProvider dateProvider,
            IPublishEndpoint publishEndpoint, 
            IVideoStorageClient videoStorage)
        {
            _currentUser = currentUser;
            _dataSource = dataSource;
            _dateProvider = dateProvider;
            _publishEndpoint = publishEndpoint;
            _videoStorage = videoStorage;
        }

        public async Task<Guid> Handle(Command request, CancellationToken cancellationToken)
        {
            // Upload to blob storage:
            var (videoId, blobName, fileSize) = await UploadVideoAsync(request.VideoFile, cancellationToken);
            
            // TODO: Don't wait for upload to complete
            // Save video to db
            var now = _dateProvider.UtcNow;
            await SaveVideo(videoId, request, blobName, fileSize, now, cancellationToken);

            await _publishEndpoint.Publish(new VideoUploadedEvent
            {
                VideoId = videoId,
                BlobName = blobName,
                Title = request.Title,
                Description = request.Description,
                OwnerId = _currentUser.Id,
                FileName = request.VideoFile.Name,
                ContentType = request.VideoFile.GetContentType(),
                SizeInBytes = fileSize,
                UploadedAt = now
            }, cancellationToken);

            return videoId;
        }

        private async Task<(Guid videoId, string BlobName, long FileSize)> UploadVideoAsync(IBrowserFile file, CancellationToken cancellationToken)
        {
            await using var stream = file.OpenReadStream(MaxFileSize, cancellationToken);
            return await _videoStorage.UploadAsync(stream, file.Name, cancellationToken);
        }

        private Task SaveVideo(
            Guid videoId,
            Command request,
            string blobName, 
            long fileSize,
            DateTimeOffset uploadedAt,
            CancellationToken cancellationToken)
        {
            // Serialize mention metadata to JSON
            var descriptionMentionsJson = request.DescriptionMentions != null && request.DescriptionMentions.Count > 0
                ? JsonSerializer.Serialize(request.DescriptionMentions)
                : null;

            return Save(
                videoId,
                request.VideoFile,
                fileSize,
                blobName,
                request.Title,
                request.Description,
                descriptionMentionsJson,
                request.VideoDate,
                uploadedAt,
                cancellationToken
            );
        }

        private async Task Save(
            Guid videoId, 
            IBrowserFile file, 
            long fileSize,
            string blobName,
            string title, 
            string? description, 
            string? descriptionMentionsJson,
            DateTime? videoDate, 
            DateTimeOffset uploadedAt,
            CancellationToken cancellationToken)
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            const string sql = 
                """
                INSERT INTO videos (
                    id,
                    blob_name, 
                    title, 
                    description, 
                    description_mentions, 
                    video_date, 
                    file_name, 
                    content_type, 
                    size_in_bytes, 
                    owner_id, 
                    uploaded_at,
                    updated_at
                ) VALUES (
                    @id, 
                    @blob_name, 
                    @title,
                    @description,
                    @description_mentions::jsonb, 
                    @video_date,
                    @file_name, 
                    @content_type, 
                    @size_in_bytes,
                    @owner_id,
                    @uploaded_at,
                    @updated_at
                )
                """;

            await connection.ExecuteAsync(sql, new
            {
                id = videoId,
                blob_name = blobName,
                title = title,
                description = description,
                description_mentions = descriptionMentionsJson,
                video_date = videoDate,
                file_name = file.Name,
                content_type = file.GetContentType(),
                size_in_bytes = fileSize,
                owner_id = _currentUser.Id,
                uploaded_at = uploadedAt,
                updated_at = uploadedAt
            });
        }
    }
    
    private static string GetContentType(this IBrowserFile file)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".webm" => "video/webm",
            ".avi" => "video/x-msvideo",
            ".wmv" => "video/x-ms-wmv",
            _ => "application/octet-stream"
        };
    }
}
