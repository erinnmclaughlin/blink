using Blink.Messaging;
using Dapper;
using MassTransit;
using MediatR;
using Npgsql;
using System.Security.Claims;

namespace Blink.Web.Components.Pages.Videos.Upload;

internal sealed class RegisterUploadedVideoCommandHandler : IRequestHandler<RegisterUploadedVideoCommand>
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPublishEndpoint _publishEndpoint;

    public RegisterUploadedVideoCommandHandler(
        NpgsqlDataSource dataSource,
        IHttpContextAccessor httpContextAccessor,
        IPublishEndpoint publishEndpoint)
    {
        _dataSource = dataSource;
        _httpContextAccessor = httpContextAccessor;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Handle(RegisterUploadedVideoCommand request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var video = new
        {
            id = Guid.NewGuid(),
            blob_name = request.BlobName,
            title = !string.IsNullOrWhiteSpace(request.Title) ? request.Title : Path.GetFileNameWithoutExtension(request.FileName),
            description = request.Description,
            video_date = request.VideoDate,
            file_name = request.FileName,
            content_type = request.GetContentType(),
            size_in_bytes = request.SizeInBytes,
            owner_id = GetCurrentUserId(),
            uploaded_at = now,
            updated_at = now
        };

        await using  (var connection = await _dataSource.OpenConnectionAsync(cancellationToken))
        {
            const string sql = """
                INSERT INTO videos (
                    id, blob_name, title, description, video_date, 
                    file_name, content_type, size_in_bytes, 
                    owner_id, uploaded_at, updated_at
                ) VALUES (
                    @id, @blob_name, @title, @description, @video_date,
                    @file_name, @content_type, @size_in_bytes,
                    @owner_id, @uploaded_at, @updated_at
                )
                """;

            await connection.ExecuteAsync(sql, video);
        }

        await _publishEndpoint.Publish(new VideoUploadedEvent
        {
            VideoId = video.id,
            BlobName = request.BlobName,
            Title = video.title,
            FileName = request.FileName,
            ContentType = video.content_type,
            OwnerId = video.owner_id,
            UploadedAt = now
        }, cancellationToken);
    }

    private string GetCurrentUserId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        
        if (user?.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("User is not authenticated");
        }

        // Try to get the user ID from the 'sub' claim (standard OIDC claim)
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                         ?? user.FindFirst("sub")
                         ?? throw new InvalidOperationException("User ID not found in claims");

        return userIdClaim.Value;
    }
}
