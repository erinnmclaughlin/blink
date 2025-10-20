using Blink.Database;
using Blink.Messaging;
using MassTransit;

namespace Blink.Web.Videos.Consumers;

public sealed class VideoMetadataExtractedConsumer : IConsumer<VideoMetadataExtracted>
{
    private readonly IBlinkUnitOfWork _unitOfWork;

    public VideoMetadataExtractedConsumer(IBlinkUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<VideoMetadataExtracted> context)
    {
        _unitOfWork.Videos.UpdateMetaData(context.Message.VideoId, context.Message.Metadata);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}
