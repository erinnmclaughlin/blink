using Blink.VideosApi.Contracts.GetByBlobName;
using Microsoft.AspNetCore.Components;

namespace Blink.WebApp.Components.Pages;
public sealed partial class VideoWatch
{
    private readonly BlinkApiClient _apiClient;
    private readonly ILogger<VideoWatch> _logger;

    [Parameter]
    public string BlobName { get; set; } = string.Empty;

    private VideoDetailDto? Video { get; set; }
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
            Video = await _apiClient.GetVideoAsync(BlobName);

            if (Video == null)
            {
                ErrorMessage = "Video not found.";
            }
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

    private static string GetDisplayTitle(VideoDetailDto video)
    {
        return !string.IsNullOrWhiteSpace(video.Title) ? video.Title : "[No Title]";
    }
}