using MediatR;

namespace Blink.WebApi.Videos.Delete;

public sealed class DeleteVideoCommandHandler : IRequestHandler<DeleteVideoCommand, DeleteVideoResponse>
{
    private readonly IVideoStorageClient _videoStorageClient;
    private readonly ILogger<DeleteVideoCommandHandler> _logger;

    public DeleteVideoCommandHandler(IVideoStorageClient videoStorageClient, ILogger<DeleteVideoCommandHandler> logger)
    {
        _videoStorageClient = videoStorageClient;
        _logger = logger;
    }

    public async Task<DeleteVideoResponse> Handle(DeleteVideoCommand request, CancellationToken cancellationToken)
    {
        var wasDeleted = await _videoStorageClient.DeleteAsync(request.BlobName, cancellationToken);
        
        if (!wasDeleted)
        {
            _logger.LogWarning("Video not found for deletion: {BlobName}", request.BlobName);
            throw new FileNotFoundException($"Video not found: {request.BlobName}");
        }

        _logger.LogInformation("Video deleted successfully: {BlobName}", request.BlobName);
        
        return new DeleteVideoResponse 
        { 
            Success = true, 
            Message = "Video deleted successfully",
            BlobName = request.BlobName
        };
    }
}

