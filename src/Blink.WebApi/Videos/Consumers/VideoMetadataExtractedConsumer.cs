using Blink.Messaging;
using MassTransit;

namespace Blink.WebApi.Videos.Consumers;

public sealed class VideoMetadataExtractedConsumer : IConsumer<VideoMetadataExtracted>
{
    private readonly IVideoRepository _videoRepository;

    public VideoMetadataExtractedConsumer(IVideoRepository videoRepository)
    {
        _videoRepository = videoRepository;
    }

    public async Task Consume(ConsumeContext<VideoMetadataExtracted> context)
    {
        await _videoRepository.UpdateMetadataAsync(context.Message.VideoBlobName, context.Message.Metadata);
    }
}
