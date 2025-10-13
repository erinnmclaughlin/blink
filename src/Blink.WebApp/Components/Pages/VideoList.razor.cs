using Blink.VideosApi.Contracts.List;

namespace Blink.WebApp.Components.Pages;

public sealed partial class VideoList
{
    private readonly BlinkApiClient _apiClient;
    private readonly ILogger<VideoList> _logger;

    private List<VideoSummaryDto> Videos { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }
    private Dictionary<string, string?> ThumbnailUrls { get; set; } = new();

    public VideoList(BlinkApiClient apiClient, ILogger<VideoList> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadVideosAsync();
    }

    private async Task LoadVideosAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            Videos = await _apiClient.GetVideosAsync();
            _logger.LogInformation("Loaded {Count} videos", Videos.Count);

            // Load thumbnail URLs for videos that have thumbnails
            ThumbnailUrls.Clear();
            foreach (var video in Videos.Where(v => !string.IsNullOrEmpty(v.ThumbnailBlobName)))
            {
                try
                {
                    var urlResponse = await _apiClient.GetVideoUrlWithThumbnailAsync(video.VideoBlobName);
                    ThumbnailUrls[video.VideoBlobName] = urlResponse.ThumbnailUrl;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load thumbnail URL for video {BlobName}", video.VideoBlobName);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load videos: {ex.Message}";
            _logger.LogError(ex, "Error loading videos");
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private static string GetDisplayTitle(VideoSummaryDto video)
    {
        return !string.IsNullOrWhiteSpace(video.Title) ? video.Title : "[No Title]";
    }

    private static string GetWatchUrl(VideoSummaryDto video)
    {
        return $"/videos/watch/{Uri.EscapeDataString(video.VideoBlobName)}";
    }

    private static bool HasThumbnail(VideoSummaryDto video)
    {
        return !string.IsNullOrEmpty(video.ThumbnailBlobName);
    }

    private string? GetThumbnailUrl(VideoSummaryDto video)
    {
        if (string.IsNullOrEmpty(video.ThumbnailBlobName))
            return null;

        return ThumbnailUrls.GetValueOrDefault(video.VideoBlobName);
    }
}