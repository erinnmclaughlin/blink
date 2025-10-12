namespace Blink.WebApi.Videos;

/// <summary>
/// IFormFile implementation that streams file content without buffering to memory.
/// </summary>
public sealed class StreamingFormFile : IFormFile
{
    private readonly Stream _stream;

    public StreamingFormFile(Stream stream, string fileName, string? contentType)
    {
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
        Name = fileName;
        ContentType = contentType ?? "application/octet-stream";
        
        // For streaming, we don't know the length upfront
        // The stream will be read directly by Azure Blob Storage
        Length = 0; 
    }

    public string ContentType { get; }
    
    public string ContentDisposition => $"form-data; name=\"{Name}\"; filename=\"{FileName}\"";
    
    public IHeaderDictionary Headers => new HeaderDictionary();
    
    public long Length { get; }
    
    public string Name { get; }
    
    public string FileName { get; }

    public Stream OpenReadStream() => _stream;

    public void CopyTo(Stream target)
    {
        _stream.CopyTo(target);
    }

    public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default)
    {
        return _stream.CopyToAsync(target, cancellationToken);
    }
}

