namespace Blink;

public interface IGuidGenerator
{
    Guid NewGuid();
}

public sealed class GuidGenerator : IGuidGenerator
{
    public Guid NewGuid() => Guid.CreateVersion7();
}
