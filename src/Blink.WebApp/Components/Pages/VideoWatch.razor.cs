using Microsoft.AspNetCore.Components;

namespace Blink.WebApp.Components.Pages;
public sealed partial class VideoWatch
{
    private readonly BlinkApiClient _apiClient;
    private readonly ILogger<VideoWatch> _logger;

    [Parameter]
    public string BlobName { get; set; } = string.Empty;

    private string? CurrentVideoUrl { get; set; }
    private VideoInfo? Video { get; set; }
    private bool IsLoadingVideo { get; set; } = true;
    private string? ErrorMessage { get; set; }

    public VideoWatch(BlinkApiClient apiClient, ILogger<VideoWatch> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadVideoAsync();
    }

    private async Task LoadVideoAsync()
    {
        if (string.IsNullOrEmpty(BlobName))
        {
            ErrorMessage = "No video specified.";
            IsLoadingVideo = false;
            return;
        }

        IsLoadingVideo = true;
        ErrorMessage = null;

        try
        {
            // TODO: Add "GetVideoByBlobName" API to avoid fetching all videos. This endpoint should also return the video URL.

            // First, get the list of videos to find details about this video
            var videos = await _apiClient.GetVideosAsync();
            Video = videos.FirstOrDefault(v => v.BlobName == BlobName);

            if (Video == null)
            {
                ErrorMessage = "Video not found.";
                return;
            }

            // Get the video URL
            CurrentVideoUrl = await _apiClient.GetVideoUrlAsync(BlobName);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load video: {ex.Message}";
            _logger.LogError(ex, "Error loading video: {BlobName}", BlobName);
        }
        finally
        {
            IsLoadingVideo = false;
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string GetDisplayTitle(VideoInfo video)
    {
        return !string.IsNullOrWhiteSpace(video.Title) ? video.Title : video.FileName;
    }
}