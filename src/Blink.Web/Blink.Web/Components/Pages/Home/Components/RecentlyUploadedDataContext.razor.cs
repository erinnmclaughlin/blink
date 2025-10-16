using Blink.VideosApi.Contracts.GetRecentUploads;
using Blink.VideosApi.Contracts.GetUrl;
using MediatR;
using Microsoft.AspNetCore.Components;
using System.Text;

namespace Blink.Web.Components.Pages.Home.Components;

public sealed partial class RecentlyUploadedDataContext
{
    private readonly ISender _sender;

    private List<RecentlyUploadedVideoVm>? Videos { get; set; }

    [Parameter]
    public RenderFragment<IReadOnlyList<RecentlyUploadedVideoVm>>? ChildContent { get; set; }

    [Parameter]
    public RenderFragment? LoadingContent { get; set; }

    public RecentlyUploadedDataContext(ISender sender)
    {
        _sender = sender;
    }

    protected override async Task OnInitializedAsync()
    {
        var videos = await _sender.Send(new GetRecentUploadsQuery());

        Videos = [];
        foreach (var video in videos)
        {
            var urls = await _sender.Send(new GetVideoUrlQuery { BlobName = video.BlobName });
            Videos.Add(new RecentlyUploadedVideoVm
            {
                Id = video.Id,
                Title = video.Title,
                DurationDisplayText = GetDurationDisplayText(video.DurationInSeconds),
                SizeDisplayText = GetSizeDisplayText(video.SizeInBytes),
                ThumbnailUrl = urls?.ThumbnailUrl,
                UploadedAt = video.UploadedAt,
                VideoDate = video.VideoDate
            });
            StateHasChanged();
        }
    }

    private static string? GetDurationDisplayText(double? durationInSections)
    {
        if (durationInSections is null)
        {
            return null;
        }

        var ts = TimeSpan.FromSeconds(durationInSections.Value);

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

public sealed record RecentlyUploadedVideoVm
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public required string? DurationDisplayText { get; init; }
    public required string SizeDisplayText { get; init; }
    public required string? ThumbnailUrl { get; init; }
    public required DateTimeOffset UploadedAt { get; init; }
    public required DateOnly? VideoDate { get; init; }
}