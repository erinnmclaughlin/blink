using System.Net.Http.Json;
using Blink.VideosApi.Contracts;
using MediatR;

namespace Blink.Web.Client;

public sealed class HttpSender(HttpClient httpClient) : ISender
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken)
    {
        if (request is IBlinkRequest<TResponse> blinkRequest)
        {
            var response = await httpClient.SendAsync(blinkRequest.ToHttpRequestMessage(), cancellationToken);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken) ?? default!;
        }
        
        throw new NotImplementedException();
    }

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = new CancellationToken()) where TRequest : IRequest
    {
        if (request is IBlinkRequest blinkRequest)
        {
            var response = await httpClient.SendAsync(blinkRequest.ToHttpRequestMessage(), cancellationToken);
            response.EnsureSuccessStatusCode();
            return;
        }
        
        throw new NotImplementedException();
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }
}