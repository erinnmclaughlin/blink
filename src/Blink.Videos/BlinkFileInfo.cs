namespace Blink.Videos;

public sealed record BlinkFileInfo
{
    /// <summary>
    /// The name of the file in blob storage.
    /// </summary>
    public string BlobName { get; }
    
    /// <summary>
    /// The name of the file, including the extension.
    /// </summary>
    public string FileName { get; }
    
    /// <summary>
    /// The size of the file in bytes.
    /// </summary>
    public long FileSize { get; }

    /// <summary>
    /// The content type of the file (e.g., "video/mp4").
    /// </summary>
    public string ContentType => Path.GetExtension(FileName).ToLowerInvariant() switch
    {
        ".mp4" => "video/mp4",
        ".webm" => "video/webm",
        ".avi" => "video/x-msvideo",
        ".wmv" => "video/x-ms-wmv",
        _ => "application/octet-stream"
    };

    public BlinkFileInfo(Guid videoId, string fileName, long fileSize)
    {
        BlobName = $"{videoId}_{fileName}";
        FileName = fileName;
        FileSize = fileSize;
    }
}
