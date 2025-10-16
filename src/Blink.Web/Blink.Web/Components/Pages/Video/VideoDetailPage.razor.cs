using Blink.Storage;
using Blink.VideosApi.Contracts.GetById;
using MediatR;
using Microsoft.AspNetCore.Components;
using System.Text;

namespace Blink.Web.Components.Pages.Video;

public sealed partial class VideoDetailPage
{
    private VideoDetailDto? Video { get; set; }
    private string? ThumbnailUrl { get; set; }
    private string? WatchUrl { get; set; }

    [Parameter]
    public Guid VideoId { get; set; }

    [Inject]
    private ISender Sender { get; set; } = default!;

    [Inject]
    private IVideoStorageClient VideoStorageClient { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        Video = await Sender.Send(new GetVideoByIdQuery(VideoId));

        if (Video is not null)
        {
            WatchUrl = await VideoStorageClient.GetUrlAsync(Video.BlobName);
        }

        if (Video?.ThumbnailBlobName is not null)
        {
            ThumbnailUrl = await VideoStorageClient.GetThumbnailUrlAsync(Video.ThumbnailBlobName);
        }
    }

    private static string GetDurationDisplayText(double durationInSeconds)
    {
        var ts = TimeSpan.FromSeconds(durationInSeconds);

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

