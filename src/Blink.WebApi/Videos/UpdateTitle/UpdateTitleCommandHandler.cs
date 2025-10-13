using Blink.VideosApi.Contracts.UpdateTitle;
using MediatR;

namespace Blink.WebApi.Videos.UpdateTitle;

public sealed class UpdateTitleCommandHandler : IRequestHandler<UpdateTitleCommand, UpdateTitleResponse>
{
    private readonly IVideoRepository _videoRepository;
    private readonly ILogger<UpdateTitleCommandHandler> _logger;

    public UpdateTitleCommandHandler(IVideoRepository videoRepository, ILogger<UpdateTitleCommandHandler> logger)
    {
        _videoRepository = videoRepository;
        _logger = logger;
    }

    public async Task<UpdateTitleResponse> Handle(UpdateTitleCommand request, CancellationToken cancellationToken)
    {
        // Get the video from database
        var video = await _videoRepository.GetByBlobNameAsync(request.BlobName, cancellationToken);
        
        if (video == null)
        {
            _logger.LogWarning("Video not found for title update: {BlobName}", request.BlobName);
            throw new FileNotFoundException($"Video not found: {request.BlobName}");
        }

        // Update title and timestamp
        video.Title = request.Title;
        video.UpdatedAt = DateTime.UtcNow;

        var wasUpdated = await _videoRepository.UpdateAsync(video, cancellationToken);
        
        if (!wasUpdated)
        {
            _logger.LogWarning("Failed to update video title in database: {BlobName}", request.BlobName);
            throw new InvalidOperationException($"Failed to update video: {request.BlobName}");
        }

        _logger.LogInformation("Video title updated successfully in database: {BlobName}, Title: {Title}", request.BlobName, request.Title);
        
        return new UpdateTitleResponse 
        { 
            Success = true, 
            Message = "Video title updated successfully",
            BlobName = request.BlobName,
            Title = request.Title
        };
    }
}

