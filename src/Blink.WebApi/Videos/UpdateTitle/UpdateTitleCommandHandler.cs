using MediatR;

namespace Blink.WebApi.Videos.UpdateTitle;

public sealed class UpdateTitleCommandHandler : IRequestHandler<UpdateTitleCommand, UpdateTitleResponse>
{
    private readonly IVideoStorageClient _videoStorageClient;
    private readonly ILogger<UpdateTitleCommandHandler> _logger;

    public UpdateTitleCommandHandler(IVideoStorageClient videoStorageClient, ILogger<UpdateTitleCommandHandler> logger)
    {
        _videoStorageClient = videoStorageClient;
        _logger = logger;
    }

    public async Task<UpdateTitleResponse> Handle(UpdateTitleCommand request, CancellationToken cancellationToken)
    {
        var wasUpdated = await _videoStorageClient.UpdateTitleAsync(request.BlobName, request.Title, cancellationToken);
        
        if (!wasUpdated)
        {
            _logger.LogWarning("Video not found for title update: {BlobName}", request.BlobName);
            throw new FileNotFoundException($"Video not found: {request.BlobName}");
        }

        _logger.LogInformation("Video title updated successfully: {BlobName}, Title: {Title}", request.BlobName, request.Title);
        
        return new UpdateTitleResponse 
        { 
            Success = true, 
            Message = "Video title updated successfully",
            BlobName = request.BlobName,
            Title = request.Title
        };
    }
}

