using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;
using System.Text;

namespace Blink.WebApi.Videos.Upload;

/// <summary>
/// Result of parsing multipart form data
/// </summary>
public sealed record MultipartFormData
{
    public IFormFile? File { get; init; }
    public Dictionary<string, string> Fields { get; init; } = new();
}

/// <summary>
/// Parses multipart form data to extract file uploads and form fields
/// </summary>
public interface IMultipartFormFileParser
{
    /// <summary>
    /// Parses the request body to extract the uploaded file and form fields
    /// </summary>
    Task<MultipartFormData> ParseAsync(HttpContext context, CancellationToken cancellationToken = default);
}

public sealed class MultipartFormFileParser : IMultipartFormFileParser
{
    private readonly ILogger<MultipartFormFileParser> _logger;

    public MultipartFormFileParser(ILogger<MultipartFormFileParser> logger)
    {
        _logger = logger;
    }

    public async Task<MultipartFormData> ParseAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        var result = new MultipartFormData { Fields = new Dictionary<string, string>() };

        if (!context.Request.HasFormContentType)
        {
            _logger.LogWarning("Request does not have form content type: {ContentType}", context.Request.ContentType);
            return result;
        }

        if (!MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var mediaTypeHeader) ||
            string.IsNullOrEmpty(mediaTypeHeader.Boundary.Value))
        {
            _logger.LogWarning("Invalid content type or missing boundary: {ContentType}", context.Request.ContentType);
            return result;
        }

        var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary.Value).Value;
        if (string.IsNullOrWhiteSpace(boundary))
        {
            _logger.LogWarning("Boundary value is empty or whitespace");
            return result;
        }

        _logger.LogInformation("Reading multipart data with boundary: {Boundary}", boundary);

        var reader = new MultipartReader(boundary, context.Request.Body);
        MultipartSection? section;
        IFormFile? fileToUpload = null;

        // Read sections one at a time (streaming)
        // Note: We read form fields first, then return when we encounter the file
        while ((section = await reader.ReadNextSectionAsync(cancellationToken)) != null)
        {
            var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
                section.ContentDisposition, out var contentDisposition);

            if (!hasContentDispositionHeader || contentDisposition == null)
                continue;

            if (contentDisposition.DispositionType.Equals("form-data"))
            {
                var name = contentDisposition.Name.Value;
                
                if (!string.IsNullOrEmpty(contentDisposition.FileName.Value))
                {
                    // This is a file - create the streaming form file and stop reading
                    // (the file stream must remain open and will be consumed by the upload handler)
                    var fileName = contentDisposition.FileName.Value;
                    _logger.LogInformation("Found file in multipart: {FileName}", fileName);
                    fileToUpload = new StreamingFormFile(section.Body, fileName, section.ContentType);
                    break; // Stop reading - the file stream must remain active
                }
                else if (!string.IsNullOrEmpty(name))
                {
                    // This is a regular form field
                    using var streamReader = new StreamReader(section.Body, Encoding.UTF8);
                    var value = await streamReader.ReadToEndAsync(cancellationToken);
                    result.Fields[name] = value;
                    _logger.LogInformation("Found form field: {Name} = {Value}", name, value);
                }
            }
        }

        result = result with { File = fileToUpload };

        if (result.File == null)
        {
            _logger.LogWarning("No file found in multipart form data");
        }

        return result;
    }
}

