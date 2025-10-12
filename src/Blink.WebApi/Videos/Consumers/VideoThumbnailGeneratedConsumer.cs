using Blink.Messaging;
using MassTransit;

namespace Blink.WebApi.Videos.Consumers;

public sealed class VideoThumbnailGeneratedConsumer : IConsumer<VideoThumbnailGenerated>
{
    private readonly ILogger<VideoThumbnailGeneratedConsumer> _logger;
    private readonly IVideoRepository _videoRepository;

    public VideoThumbnailGeneratedConsumer(ILogger<VideoThumbnailGeneratedConsumer> logger, IVideoRepository videoRepository)
    {
        _logger = logger;
        _videoRepository = videoRepository;
    }

    public async Task Consume(ConsumeContext<VideoThumbnailGenerated> context)
    {
        var message = context.Message;

        _logger.LogInformation("Received thumbnail generated event for video: {VideoBlobName}, Thumbnail: {ThumbnailBlobName}", message.VideoBlobName, message.ThumbnailBlobName);
        _logger.LogInformation("Saving generated thumbnail to database for video: {VideoBlobName}, Thumbnail: {ThumbnailBlobName}", message.VideoBlobName, message.ThumbnailBlobName);
        await _videoRepository.UpdateThumbnailAsync(message.VideoBlobName, message.ThumbnailBlobName);
    }
}
