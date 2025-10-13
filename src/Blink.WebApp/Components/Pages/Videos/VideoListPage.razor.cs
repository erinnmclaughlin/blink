using Blink.VideosApi.Contracts.List;

namespace Blink.WebApp.Components.Pages.Videos;

public sealed partial class VideoListPage
{
    private readonly BlinkApiClient _apiClient;
    private readonly ILogger<VideoListPage> _logger;

    private List<VideoSummaryDto> Videos { get; set; } = [];
    private bool IsLoading { get; set; } = true;
    private string? ErrorMessage { get; set; }

    public VideoListPage(BlinkApiClient apiClient, ILogger<VideoListPage> logger)
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

}