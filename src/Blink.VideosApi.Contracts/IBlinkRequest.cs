namespace Blink.VideosApi.Contracts;

public interface IBlinkRequestBase : IBaseRequest
{
    HttpRequestMessage ToHttpRequestMessage();
}

public interface IBlinkRequest : IBlinkRequestBase, IRequest
{
}
public interface IBlinkRequest<out T> : IBlinkRequestBase, IRequest<T>;
