namespace Blink.VideosApi.Contracts.List;

public sealed record ListVideosQuery : IBlinkRequest<List<VideoSummaryDto>>
{
    public HttpRequestMessage ToHttpRequestMessage()
    {
        return new HttpRequestMessage(HttpMethod.Get, "api/videos");
    }
}
