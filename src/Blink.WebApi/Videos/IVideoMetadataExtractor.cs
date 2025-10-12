using System.Diagnostics;
using System.Text.Json;

namespace Blink.WebApi.Videos;

/// <summary>
/// Service for extracting metadata from video files
/// </summary>
public interface IVideoMetadataExtractor
{
    /// <summary>
    /// Extracts metadata from a video stream
    /// </summary>
    /// <param name="videoStream">The video stream to extract metadata from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Video metadata including dimensions</returns>
    Task<VideoMetadata?> ExtractMetadataAsync(Stream videoStream, CancellationToken cancellationToken = default);
}

/// <summary>
/// Video metadata extracted from the video file
/// </summary>
public sealed record VideoMetadata
{
    public int Width { get; init; }
    public int Height { get; init; }
    public double DurationInSeconds { get; init; }
}

/// <summary>
/// Video metadata extractor using FFprobe
/// </summary>
public sealed class FFprobeMetadataExtractor : IVideoMetadataExtractor
{
    private readonly ILogger<FFprobeMetadataExtractor> _logger;

    public FFprobeMetadataExtractor(ILogger<FFprobeMetadataExtractor> logger)
    {
        _logger = logger;
    }

    public async Task<VideoMetadata?> ExtractMetadataAsync(Stream videoStream, CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"video-metadata-{Guid.NewGuid():N}.bin");

        try
        {
            // Write video stream to temp file
            _logger.LogInformation("FFprobe: writing input to temp file {Path}", tempPath);
            await using (var fs = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await videoStream.CopyToAsync(fs, cancellationToken);
            }

            // Use ffprobe to extract video metadata in JSON format
            var args = 
                $"-v error -select_streams v:0 -show_entries stream=width,height:format=duration " +
                $"-of json \"{tempPath}\"";

            _logger.LogInformation("Running FFprobe: {Args}", args);

            var psi = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            proc.Start();

            var stdoutTask = proc.StandardOutput.ReadToEndAsync();
            var stderrTask = proc.StandardError.ReadToEndAsync();

            using (cancellationToken.Register(() =>
            {
                try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
            }))
            {
                await proc.WaitForExitAsync(cancellationToken);
            }

            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            if (proc.ExitCode != 0)
            {
                _logger.LogError("FFprobe failed (code {Code}). Stderr:\n{Err}", proc.ExitCode, stderr);
                return null;
            }

            // Parse JSON output
            var jsonDoc = JsonDocument.Parse(stdout);
            
            // Extract width and height from streams
            var streams = jsonDoc.RootElement.GetProperty("streams");
            if (streams.GetArrayLength() == 0)
            {
                _logger.LogWarning("No video stream found in file");
                return null;
            }

            var firstStream = streams[0];
            if (!firstStream.TryGetProperty("width", out var widthProp) || 
                !firstStream.TryGetProperty("height", out var heightProp))
            {
                _logger.LogWarning("Width or height not found in video metadata");
                return null;
            }

            int width = widthProp.GetInt32();
            int height = heightProp.GetInt32();

            // Extract duration from format
            double duration = 0;
            if (jsonDoc.RootElement.TryGetProperty("format", out var formatProp) &&
                formatProp.TryGetProperty("duration", out var durationProp))
            {
                if (durationProp.ValueKind == JsonValueKind.String)
                {
                    double.TryParse(durationProp.GetString(), out duration);
                }
                else if (durationProp.ValueKind == JsonValueKind.Number)
                {
                    duration = durationProp.GetDouble();
                }
            }

            _logger.LogInformation("Extracted video metadata: {Width}x{Height}, Duration: {Duration}s", 
                width, height, duration);

            return new VideoMetadata
            {
                Width = width,
                Height = height,
                DurationInSeconds = duration
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting video metadata");
            return null;
        }
        finally
        {
            // Best-effort cleanup
            try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* ignore */ }
        }
    }
}

