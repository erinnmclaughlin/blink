namespace Blink.Web.Services;

public interface IDateProvider
{
    DateTimeOffset UtcNow { get; }
}

public sealed class DateProvider : IDateProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
