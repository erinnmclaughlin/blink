using Blink.Messaging;
using Blink.Storage;
using MassTransit;

namespace Blink.VideoSummarizer.Consumers;

/// <summary>
/// Consumer that generates AI summaries for uploaded videos
/// </summary>
public sealed class VideoSummaryGenerator : IConsumer<VideoUploadedEvent>
{
    private readonly ILogger<VideoSummaryGenerator> _logger;
    private readonly IVideoSummarizer _videoSummarizer;
    private readonly IVideoStorageClient _videoStorageClient;

    public VideoSummaryGenerator(
        ILogger<VideoSummaryGenerator> logger,
        IVideoSummarizer videoSummarizer,
        IVideoStorageClient videoStorageClient)
    {
        _logger = logger;
        _videoSummarizer = videoSummarizer;
        _videoStorageClient = videoStorageClient;
    }

    public async Task Consume(ConsumeContext<VideoUploadedEvent> context)
    {
        var message = context.Message;
        var videoBlobName = message.BlobName;

        _logger.LogInformation(
            "Generating AI summary for video: {VideoId} - {Title}",
            message.VideoId,
            message.Title);

        var summary = await GenerateSummaryAsync(message);

        if (summary is not null)
        {
            _logger.LogInformation(
                "Successfully generated summary for video {VideoId}: {Summary}",
                message.VideoId,
                summary.Length > 100 ? summary[..100] + "..." : summary);

            // TODO: Store the summary in the database or publish an event
            // For now, we just log it
        }
        else
        {
            _logger.LogWarning(
                "Failed to generate summary for video {VideoId}",
                message.VideoId);
        }
    }

    private async Task<string?> GenerateSummaryAsync(VideoUploadedEvent videoEvent)
    {
        try
        {
            _logger.LogInformation("Downloading video for summary generation: {BlobName}", videoEvent.BlobName);

            using var videoStream = await _videoStorageClient.DownloadAsync(videoEvent.BlobName);

            var metadata = new VideoMetadata
            {
                Title = videoEvent.Title,
                Description = videoEvent.Description,
                FileName = videoEvent.FileName,
                ContentType = videoEvent.ContentType
            };

            var summary = await _videoSummarizer.GenerateSummaryAsync(videoStream, metadata);

            if (summary is not null)
            {
                _logger.LogInformation(
                    "AI summary generated for video: {BlobName}",
                    videoEvent.BlobName);
            }
            else
            {
                _logger.LogWarning(
                    "Could not generate AI summary for video: {BlobName}",
                    videoEvent.BlobName);
            }

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error generating AI summary for video: {BlobName}",
                videoEvent.BlobName);
            return null;
        }
    }
}

