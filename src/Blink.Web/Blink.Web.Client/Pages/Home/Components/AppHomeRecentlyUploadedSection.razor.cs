using Blink.VideosApi.Contracts.GetUrl;
using Blink.VideosApi.Contracts.List;
using MediatR;
using Microsoft.AspNetCore.Components;
using System.Text;

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

    private static string GetDurationDisplayText(double durationInSections)
    {
        var ts = TimeSpan.FromSeconds(durationInSections);

        var sb = new StringBuilder();

        if (ts.Hours > 0)
        {
            sb.Append(ts.Hours);
            sb.Append(':');
            sb.Append(ts.Minutes.ToString("D2"));
            sb.Append(':');
            sb.Append(ts.Seconds.ToString("D2"));

        }
        else
        {
            sb.Append(ts.Minutes);
            sb.Append(':');
            sb.Append(ts.Seconds.ToString("D2"));
        }

        return sb.ToString();
    }

    private static string GetSizeDisplayText(long sizeInBytes)
    {
        if (sizeInBytes < 1024)
        {
            return $"{sizeInBytes} B";
        }
        else if (sizeInBytes < 1024 * 1024)
        {
            return $"{sizeInBytes / 1024.0:F2} KB";
        }
        else if (sizeInBytes < 1024 * 1024 * 1024)
        {
            return $"{sizeInBytes / (1024.0 * 1024.0):F2} MB";
        }
        else
        {
            return $"{sizeInBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}