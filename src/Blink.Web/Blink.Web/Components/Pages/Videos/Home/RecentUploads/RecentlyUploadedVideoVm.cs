using System.Text;

namespace Blink.Web.Components.Pages.Videos.Home.RecentUploads;

public sealed record RecentlyUploadedVideoVm
{
    public required Guid Id { get; init; }
    public required string BlobName { get; init; }
    public required string Title { get; init; }
    public required double? DurationInSeconds { get; init; }
    public required long SizeInBytes { get; init; }
    public required string? ThumbnailBlobName { get; init; }
    public required DateTimeOffset UploadedAt { get; init; }
    public required DateOnly? VideoDate { get; init; }

    public string? GetDurationDisplayText()
    {
        if (DurationInSeconds is null)
        {
            return null;
        }

        var ts = TimeSpan.FromSeconds(DurationInSeconds.Value);

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

    public string GetSizeDisplayText()
    {
        if (SizeInBytes < 1024)
        {
            return $"{SizeInBytes} B";
        }
        else if (SizeInBytes < 1024 * 1024)
        {
            return $"{SizeInBytes / 1024.0:F2} KB";
        }
        else if (SizeInBytes < 1024 * 1024 * 1024)
        {
            return $"{SizeInBytes / (1024.0 * 1024.0):F2} MB";
        }
        else
        {
            return $"{SizeInBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
        }
    }
}