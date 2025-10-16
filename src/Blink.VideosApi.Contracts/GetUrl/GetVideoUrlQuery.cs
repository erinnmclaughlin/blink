namespace Blink.VideosApi.Contracts.GetUrl;

public sealed record GetVideoUrlQuery : IBlinkRequest<VideoUrlResponse>
{
    public required string BlobName { get; init; }
    
    public HttpRequestMessage ToHttpRequestMessage()
    {
        return new HttpRequestMessage(HttpMethod.Get, $"api/videos/{BlobName}/url");
    }
}
