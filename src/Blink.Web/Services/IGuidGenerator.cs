namespace Blink.Web.Services;

public interface IGuidGenerator
{
    Guid NewGuid();
}

public sealed class GuidGenerator
{
    public Guid NewGuid() => Guid.CreateVersion7();
}
