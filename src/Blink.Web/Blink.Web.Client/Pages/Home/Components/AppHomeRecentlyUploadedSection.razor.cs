using Blink.VideosApi.Contracts.GetUrl;
using Blink.VideosApi.Contracts.List;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace Blink.Web.Client.Pages.Home.Components;

public sealed partial class AppHomeRecentlyUploadedSection
{
    private readonly ISender _sender;
    
    [PersistentState]
    public List<VideoSummaryDto>? Videos { get; set; }
    
    [PersistentState]
    public Dictionary<string, VideoUrlResponse?>? VideoUrls { get; set; }
    
    public AppHomeRecentlyUploadedSection(ISender sender)
    {
        _sender = sender;
    }

    protected override async Task OnInitializedAsync()
    {
        Videos ??= await _sender.Send(new ListVideosQuery());

        if (VideoUrls is null)
        {
            VideoUrls = [];
            foreach (var video in Videos)
            {
                VideoUrls[video.VideoBlobName] = await _sender.Send(new GetVideoUrlQuery { BlobName = video.VideoBlobName });
            }
        }
    }
}