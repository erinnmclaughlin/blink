using Blink.VideosApi.Contracts.UpdateMetadata;
using MediatR;

namespace Blink.WebApi.Videos.UpdateMetadata;

public sealed class UpdateMetadataCommandHandler : IRequestHandler<UpdateMetadataCommand, UpdateMetadataResponse>
{
    private readonly IVideoRepository _videoRepository;
    private readonly ILogger<UpdateMetadataCommandHandler> _logger;

    public UpdateMetadataCommandHandler(IVideoRepository videoRepository, ILogger<UpdateMetadataCommandHandler> logger)
    {
        _videoRepository = videoRepository;
        _logger = logger;
    }

    public async Task<UpdateMetadataResponse> Handle(UpdateMetadataCommand request, CancellationToken cancellationToken)
    {
        // Get the video from database
        var video = await _videoRepository.GetByBlobNameAsync(request.BlobName, cancellationToken);
        
        if (video == null)
        {
            _logger.LogWarning("Video not found for metadata update: {BlobName}", request.BlobName);
            throw new FileNotFoundException($"Video not found: {request.BlobName}");
        }

        // Update metadata and timestamp
        video.Title = request.Title;
        video.Description = request.Description;
        video.VideoDate = request.VideoDate;
        video.UpdatedAt = DateTime.UtcNow;

        var wasUpdated = await _videoRepository.UpdateAsync(video, cancellationToken);
        
        if (!wasUpdated)
        {
            _logger.LogWarning("Failed to update video metadata in database: {BlobName}", request.BlobName);
            throw new InvalidOperationException($"Failed to update video: {request.BlobName}");
        }

        _logger.LogInformation("Video metadata updated successfully in database: {BlobName}", request.BlobName);
        
        return new UpdateMetadataResponse 
        { 
            Success = true, 
            Message = "Video metadata updated successfully",
            BlobName = request.BlobName,
            Title = request.Title,
            Description = request.Description,
            VideoDate = request.VideoDate
        };
    }
}

