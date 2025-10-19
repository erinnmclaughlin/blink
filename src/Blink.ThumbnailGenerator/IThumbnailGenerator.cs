using System.Diagnostics;

namespace Blink.ThumbnailGenerator;

/// <summary>
/// Service for generating video thumbnails
/// </summary>
public interface IThumbnailGenerator
{
    /// <summary>
    /// Generates a thumbnail from a video stream
    /// </summary>
    /// <param name="videoStream">The video stream to extract thumbnail from</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>A stream containing the thumbnail image (JPEG format)</returns>
    Task<Stream> GenerateThumbnailAsync(Stream videoStream, CancellationToken cancellationToken = default);
}

/// <summary>
/// Simple thumbnail generator that creates a placeholder image
/// In production, this would use FFmpeg or similar to extract a frame from the video
/// </summary>
public sealed class ThumbnailGenerator : IThumbnailGenerator
{
    private const int JpegQuality = 15; // 2-31, lower is better quality

    private readonly ILogger<ThumbnailGenerator> _logger;

    public ThumbnailGenerator(ILogger<ThumbnailGenerator> logger)
    {
        _logger = logger;
    }

    public async Task<Stream> GenerateThumbnailAsync(Stream videoStream, CancellationToken cancellationToken = default)
    {
        // temp files
        var inPath = Path.Combine(Path.GetTempPath(), $"thumb-in-{Guid.NewGuid():N}.bin");
        var outPath = Path.Combine(Path.GetTempPath(), $"thumb-out-{Guid.NewGuid():N}.jpg");

        try
        {
            _logger.LogInformation("FFmpeg thumbnail: writing input to temp file {Path}", inPath);
            await using (var fs = new FileStream(inPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await videoStream.CopyToAsync(fs, cancellationToken);
            }

            var args =
                $"-hide_banner -loglevel error -y " +
                $"-ss {FormatTimestamp(TimeSpan.FromSeconds(5))} -i \"{inPath}\" " +
                $"-frames:v 1 -q:v {JpegQuality} " +
                $"\"{outPath}\"";

            _logger.LogInformation("Running FFmpeg: {Args}", args);

            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            proc.Start();

            // read stderr so the buffer doesn't block
            var stderrTask = Task.Run(async () =>
            {
                var lines = new List<string>();
                while (!proc.StandardError.EndOfStream)
                {
                    var line = await proc.StandardError.ReadLineAsync();
                    if (line is not null) lines.Add(line);
                }
                return string.Join(Environment.NewLine, lines);
            }, cancellationToken);

            using (cancellationToken.Register(() =>
            {
                try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
            }))
            {
                await proc.WaitForExitAsync(cancellationToken);
            }

            string stderr = await stderrTask;
            if (proc.ExitCode != 0 || !File.Exists(outPath))
            {
                _logger.LogError("FFmpeg failed (code {Code}). Stderr:\n{Err}", proc.ExitCode, stderr);
                throw new InvalidOperationException($"FFmpeg failed with exit code {proc.ExitCode}");
            }

            // return the JPEG as a MemoryStream
            var ms = new MemoryStream(await File.ReadAllBytesAsync(outPath, cancellationToken));
            ms.Position = 0;
            _logger.LogInformation("FFmpeg thumbnail generated ({Bytes} bytes).", ms.Length);
            return ms;
        }
        finally
        {
            // best-effort cleanup
            TryDelete(inPath);
            TryDelete(outPath);
        }
    }

    private static string FormatTimestamp(TimeSpan ts)
        => $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}.{ts.Milliseconds:000}";

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }
}
