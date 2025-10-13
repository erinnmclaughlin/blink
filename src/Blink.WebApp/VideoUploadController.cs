using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Blink.WebApp;

[Authorize]
[Route("api/videos")]
public class VideoUploadController : Controller
{
    private readonly BlinkApiClient _apiClient;

    public VideoUploadController(BlinkApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpPost("upload")]
    [RequestFormLimits(MultipartBodyLengthLimit = 2_000_000_000)]
    [RequestSizeLimit(2_000_000_000)]
    public async Task<IActionResult> Upload(IFormFile videoFile, string? videoTitle, string? videoDescription, DateTime? videoDate, CancellationToken cancellationToken)
    {
        if (videoFile == null || videoFile.Length == 0)
        {
            TempData["ErrorMessage"] = "No file selected.";
            return RedirectToAction("/videos/upload");
        }

        if (videoFile.Length > 2_000_000_000)
        {
            TempData["ErrorMessage"] = "File size exceeds 2GB.";
            return Redirect("/videos/upload");
        }

        var stream = videoFile.OpenReadStream();

        var response = await _apiClient.UploadVideoAsync(
            stream,
            videoFile.FileName,
            videoTitle,
            videoDescription,
            videoDate.HasValue ? DateOnly.FromDateTime(videoDate.Value) : null,
            cancellationToken
        );

        return Redirect($"/videos/watch/{response.BlobName}");
    }
}
