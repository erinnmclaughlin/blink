namespace Blink.WebApi.Videos.Upload;

/// <summary>
/// Parses multipart form data to extract file uploads
/// </summary>
public interface IMultipartFormFileParser
{
    /// <summary>
    /// Parses the request body to extract the uploaded file
    /// </summary>
    Task<IFormFile?> ParseFileAsync(HttpContext context, CancellationToken cancellationToken = default);
}

