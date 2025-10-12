using Blink.Storage;
using MediatR;

namespace Blink.WebApi.Videos.Delete;

public sealed class DeleteVideoCommandHandler : IRequestHandler<DeleteVideoCommand, DeleteVideoResponse>
{
    private readonly IVideoStorageClient _videoStorageClient;
    private readonly IVideoRepository _videoRepository;
    private readonly ILogger<DeleteVideoCommandHandler> _logger;

    public DeleteVideoCommandHandler(
        IVideoStorageClient videoStorageClient,
        IVideoRepository videoRepository,
        ILogger<DeleteVideoCommandHandler> logger)
    {
        _videoStorageClient = videoStorageClient;
        _videoRepository = videoRepository;
        _logger = logger;
    }

    public async Task<DeleteVideoResponse> Handle(DeleteVideoCommand request, CancellationToken cancellationToken)
    {
        // Delete from blob storage first
        var wasDeletedFromStorage = await _videoStorageClient.DeleteAsync(request.BlobName, cancellationToken);
        
        if (!wasDeletedFromStorage)
        {
            _logger.LogWarning("Video not found in blob storage for deletion: {BlobName}", request.BlobName);
        }

        // Delete from database
        var wasDeletedFromDb = await _videoRepository.DeleteByBlobNameAsync(request.BlobName, cancellationToken);
        
        if (!wasDeletedFromDb && !wasDeletedFromStorage)
        {
            _logger.LogWarning("Video not found in database or storage for deletion: {BlobName}", request.BlobName);
            throw new FileNotFoundException($"Video not found: {request.BlobName}");
        }

        _logger.LogInformation("Video deleted successfully from storage and database: {BlobName}", request.BlobName);
        
        return new DeleteVideoResponse 
        { 
            Success = true, 
            Message = "Video deleted successfully",
            BlobName = request.BlobName
        };
    }
}

