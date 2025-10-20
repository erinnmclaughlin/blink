using Blink.Database;
using Blink.Messaging;
using Blink.Storage;
using Blink.Videos;
using Blink.Web.Authentication;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;

namespace Blink.Web.Videos.Features;

public static class UploadVideo
{
    // ReSharper disable once MemberCanBePrivate.Global
    public const long MaxFileSize = 2000 * 1024 * 1024; // 2GB

    public sealed record Command : IRequest<BlinkVideo>
    {
        public required IBrowserFile VideoFile { get; init; }

        public string Title { get; init; } = "[No Title]";
        public string? Description { get; init; }
        public DateOnly? VideoDate { get; init; }
    }

    public sealed class CommandHandler : IRequestHandler<Command, BlinkVideo>
    {
        private readonly ICurrentUser _currentUser;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly IBlinkUnitOfWork _unitOfWork;
        private readonly IVideoStorageClient _videoStorage;
        private readonly IBlinkVideoFactory _videoFactory;

        public CommandHandler(
            ICurrentUser currentUser,
            IPublishEndpoint publishEndpoint, 
            IBlinkUnitOfWork unitOfWork,
            IVideoStorageClient videoStorage,
            IBlinkVideoFactory videoFactory)
        {
            _currentUser = currentUser;
            _publishEndpoint = publishEndpoint;
            _videoStorage = videoStorage;
            _videoFactory = videoFactory;
            _unitOfWork = unitOfWork;
        }

        public async Task<BlinkVideo> Handle(Command request, CancellationToken cancellationToken)
        {
            await using var stream = request.VideoFile.OpenReadStream(MaxFileSize, cancellationToken);

            var video = CreateVideo(request, stream);
            
            await _videoStorage.UploadAsync(stream, video.File, cancellationToken);
            
            _unitOfWork.Videos.Add(video);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            await _publishEndpoint.Publish(new VideoUploadedEvent(video), cancellationToken);

            return video;
        }

        private BlinkVideo CreateVideo(Command request, Stream fileStream)
        {
            var video = _videoFactory.CreateNew(request.Title, request.VideoFile.Name, fileStream.Length, _currentUser.Id);

            if (!string.IsNullOrWhiteSpace(request.Description))
            {
                video.SetDescription(request.Description);
            }

            if (request.VideoDate.HasValue)
            {
                video.SetCaptureDate(request.VideoDate.Value);
            }
            
            return video;
        }
    }
}
