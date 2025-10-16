namespace Blink.VideosApi.Contracts.GetRecentUploads;

public sealed record GetRecentUploadsQuery : IBlinkRequest<List<VideoSummaryDto>>
{
    public HttpRequestMessage ToHttpRequestMessage()
    {
        return new HttpRequestMessage(HttpMethod.Get, "api/recent-uploads");
    }
}
