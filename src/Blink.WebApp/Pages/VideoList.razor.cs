namespace Blink.WebApp.Pages;
public sealed partial class VideoList
{
    private List<VideoInfo> Videos { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await LoadVideosAsync();
    }

    private async Task LoadVideosAsync()
    {
        IsLoading = true;
        ErrorMessage = null;
        StateHasChanged();

        try
        {
            Videos = await ApiClient.GetVideosAsync();
            Logger.LogInformation("Loaded {Count} videos", Videos.Count);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to load videos: {ex.Message}";
            Logger.LogError(ex, "Error loading videos");
        }
        finally
        {
            IsLoading = false;
            StateHasChanged();
        }
    }

    private static string GetWatchUrl(VideoInfo video)
    {
        return $"/videos/watch/{Uri.EscapeDataString(video.BlobName)}";
    }

    private static string GetVideoIcon(string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "video/mp4" => "MP4",
            "video/x-msvideo" => "AVI",
            "video/quicktime" => "MOV",
            "video/x-ms-wmv" => "WMV",
            "video/x-flv" => "FLV",
            "video/webm" => "WEBM",
            "video/x-matroska" => "MKV",
            _ => "VIDEO"
        };
    }

    private static string GetDisplayTitle(VideoInfo video)
    {
        return !string.IsNullOrWhiteSpace(video.Title) ? video.Title : video.FileName;
    }
}