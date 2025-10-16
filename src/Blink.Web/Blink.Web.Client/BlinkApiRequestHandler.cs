using System.Net.Http.Json;
using Blink.VideosApi.Contracts;
using MediatR;

namespace Blink.Web.Client;

public sealed class BlinkApiRequestHandler<TRequest, TResponse> : IRequestHandler<TRequest, TResponse> where TRequest : IBlinkRequest<TResponse>
{
    private readonly HttpClient _httpClient;

    public BlinkApiRequestHandler(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.SendAsync(request.ToHttpRequestMessage(), cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken) ?? default!;
    }
}
