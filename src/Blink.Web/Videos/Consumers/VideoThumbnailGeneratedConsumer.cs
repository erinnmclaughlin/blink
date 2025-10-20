using Blink.Database;
using Blink.Messaging;
using MassTransit;

namespace Blink.Web.Videos.Consumers;

public sealed class VideoThumbnailGeneratedConsumer : IConsumer<VideoThumbnailGenerated>
{
    private readonly IBlinkUnitOfWork _unitOfWork;

    public VideoThumbnailGeneratedConsumer(IBlinkUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<VideoThumbnailGenerated> context)
    {
        _unitOfWork.Videos.UpdateThumbnail(context.Message.VideoId, context.Message.ThumbnailBlobName);
        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
    }
}
