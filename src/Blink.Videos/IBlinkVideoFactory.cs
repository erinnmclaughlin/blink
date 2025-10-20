namespace Blink.Videos;

public interface IBlinkVideoFactory
{
    BlinkVideo CreateNew(string title, string fileName, long fileSize, string ownerId);
}
