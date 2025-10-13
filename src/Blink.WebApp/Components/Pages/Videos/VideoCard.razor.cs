using Blink.VideosApi.Contracts.List;
using Microsoft.AspNetCore.Components;

namespace Blink.WebApp.Components.Pages.Videos;

public sealed partial class VideoCard
{
    private readonly BlinkApiClient _apiClient;

    private string? ThumbnailUrl { get; set; }

    [Parameter, EditorRequired]
    public required VideoSummaryDto Video { get; set; }

    public VideoCard(BlinkApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    protected override async Task OnParametersSetAsync()
    {
        var urlResponse = await _apiClient.GetVideoUrlWithThumbnailAsync(Video.VideoBlobName);
        ThumbnailUrl = urlResponse.ThumbnailUrl;
    }

    private string GetDisplayTitle()
    {
        return !string.IsNullOrWhiteSpace(Video.Title) ? Video.Title : "[No Title]";
    }

    private string GetWatchUrl()
    {
        return $"/videos/watch/{Uri.EscapeDataString(Video.VideoBlobName)}";
    }
}