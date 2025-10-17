using Blink.Messaging;
using Blink.VideosApi.Contracts.Upload;
using Dapper;
using MassTransit;
using MediatR;
using Npgsql;
using System.Security.Claims;

namespace Blink.Web.Videos.RegisterUpload;

public sealed class RegisterUploadedVideoCommandHandler : IRequestHandler<RegisterUploadedVideoCommand>, IDisposable
{
    private readonly NpgsqlConnection _connection;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPublishEndpoint _publishEndpoint;

    public RegisterUploadedVideoCommandHandler(
        NpgsqlDataSource dataSource,
        IHttpContextAccessor httpContextAccessor,
        IPublishEndpoint publishEndpoint)
    {
        _connection = dataSource.CreateConnection();
        _httpContextAccessor = httpContextAccessor;
        _publishEndpoint = publishEndpoint;
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    public async Task Handle(RegisterUploadedVideoCommand request, CancellationToken cancellationToken)
    {
        await _connection.OpenAsync(cancellationToken);

        var userId = GetCurrentUserId();
        var now = DateTimeOffset.UtcNow;
        var videoId = Guid.NewGuid();

        // Determine title - use provided title or derive from filename
        var title = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title
            : Path.GetFileNameWithoutExtension(request.FileName);

        // Determine content type from filename
        var contentType = GetContentType(request.FileName);

        const string sql = @"
            INSERT INTO videos (
                id, blob_name, title, description, video_date, 
                file_name, content_type, size_in_bytes, 
                owner_id, uploaded_at, updated_at
            ) VALUES (
                @Id, @BlobName, @Title, @Description, @VideoDate,
                @FileName, @ContentType, @SizeInBytes,
                @OwnerId, @UploadedAt, @UpdatedAt
            )";

        await _connection.ExecuteAsync(sql, new
        {
            Id = videoId,
            BlobName = request.BlobName,
            Title = title,
            Description = request.Description,
            VideoDate = request.VideoDate,
            FileName = request.FileName,
            ContentType = contentType,
            SizeInBytes = request.SizeInBytes,
            OwnerId = userId,
            UploadedAt = now,
            UpdatedAt = now
        });

        await _publishEndpoint.Publish(new VideoUploadedEvent
        {
            VideoId = videoId,
            BlobName = request.BlobName,
            Title = title,
            FileName = request.FileName,
            ContentType = contentType,
            OwnerId = userId,
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

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
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

