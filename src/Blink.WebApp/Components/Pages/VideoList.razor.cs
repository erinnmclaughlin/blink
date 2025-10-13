namespace Blink.WebApp.Components.Pages;

public sealed partial class VideoList
{
    private List<VideoSummaryDto> Videos { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }
    private Dictionary<string, string?> ThumbnailUrls { get; set; } = new();

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
            Videos = await ApiClient.GetVideosAsync();
            Logger.LogInformation("Loaded {Count} videos", Videos.Count);

            // Load thumbnail URLs for videos that have thumbnails
            ThumbnailUrls.Clear();
            foreach (var video in Videos.Where(v => !string.IsNullOrEmpty(v.ThumbnailBlobName)))
            {
                try
                {
                    var urlResponse = await ApiClient.GetVideoUrlWithThumbnailAsync(video.VideoBlobName);
                    ThumbnailUrls[video.VideoBlobName] = urlResponse.ThumbnailUrl;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Failed to load thumbnail URL for video {BlobName}", video.VideoBlobName);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load videos: {ex.Message}";
            Logger.LogError(ex, "Error loading videos");
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private string GetDisplayTitle(VideoSummaryDto video)
    {
        return !string.IsNullOrWhiteSpace(video.Title) ? video.Title : "[No Title]";
    }

    private string GetWatchUrl(VideoSummaryDto video)
    {
        return $"/videos/watch/{Uri.EscapeDataString(video.VideoBlobName)}";
    }

    private string GetVideoIcon(string contentType)
    {
        return contentType.ToLower() switch
        {
            "video/mp4" => "MP4",
            "video/quicktime" => "MOV",
            "video/x-msvideo" => "AVI",
            "video/x-matroska" => "MKV",
            _ => "VIDEO"
        };
    }

    private bool HasThumbnail(VideoSummaryDto video)
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