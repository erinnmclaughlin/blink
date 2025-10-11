using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Blink.WebApi.Videos.Upload;

/// <summary>
/// Parses multipart form data to extract streaming file uploads
/// </summary>
public sealed class MultipartFormFileParser : IMultipartFormFileParser
{
    private readonly ILogger<MultipartFormFileParser> _logger;

    public MultipartFormFileParser(ILogger<MultipartFormFileParser> logger)
    {
        _logger = logger;
    }

    public async Task<IFormFile?> ParseFileAsync(HttpContext context, CancellationToken cancellationToken = default)
    {
        if (!context.Request.HasFormContentType)
        {
            _logger.LogWarning("Request does not have form content type: {ContentType}", context.Request.ContentType);
            return null;
        }

        if (!MediaTypeHeaderValue.TryParse(context.Request.ContentType, out var mediaTypeHeader) ||
            string.IsNullOrEmpty(mediaTypeHeader.Boundary.Value))
        {
            _logger.LogWarning("Invalid content type or missing boundary: {ContentType}", context.Request.ContentType);
            return null;
        }

        var boundary = HeaderUtilities.RemoveQuotes(mediaTypeHeader.Boundary.Value).Value;
        if (string.IsNullOrWhiteSpace(boundary))
        {
            _logger.LogWarning("Boundary value is empty or whitespace");
            return null;
        }

        _logger.LogInformation("Reading multipart data with boundary: {Boundary}", boundary);

        var reader = new MultipartReader(boundary, context.Request.Body);
        MultipartSection? section;

        // Read sections one at a time (streaming)
        while ((section = await reader.ReadNextSectionAsync(cancellationToken)) != null)
        {
            var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
                section.ContentDisposition, out var contentDisposition);

            if (hasContentDispositionHeader && 
                contentDisposition!.DispositionType.Equals("form-data") &&
                !string.IsNullOrEmpty(contentDisposition.FileName.Value))
            {
                // This is a file
                var fileName = contentDisposition.FileName.Value;
                _logger.LogInformation("Found file in multipart: {FileName}", fileName);
                return new StreamingFormFile(section.Body, fileName, section.ContentType);
            }
        }

        _logger.LogWarning("No file found in multipart form data");
        return null;
    }
}

