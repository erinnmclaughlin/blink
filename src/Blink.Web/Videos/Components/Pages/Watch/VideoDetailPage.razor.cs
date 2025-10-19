using Blink.Storage;
using Blink.Web.Videos.Pages.Watch;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace Blink.Web.Videos.Components.Pages.Watch;

public sealed partial class VideoDetailPage
{
    private VideoDetailVm? Video { get; set; }
    private string? ThumbnailUrl { get; set; }
    private string? WatchUrl { get; set; }

    [Parameter]
    public Guid VideoId { get; set; }

    [Inject]
    private ISender Sender { get; set; } = null!;

    [Inject]
    private IVideoStorageClient VideoStorageClient { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        Video = await Sender.Send(new GetVideoDetailQuery(VideoId));

        if (Video is not null)
        {
            WatchUrl = await VideoStorageClient.GetUrlAsync(Video.BlobName);
        }

        if (Video?.ThumbnailBlobName is not null)
        {
            ThumbnailUrl = await VideoStorageClient.GetThumbnailUrlAsync(Video.ThumbnailBlobName);
        }
    }
}
