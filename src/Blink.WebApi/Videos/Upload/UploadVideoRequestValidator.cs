using FluentValidation;

namespace Blink.WebApi.Videos.Upload;

public sealed class UploadVideoRequestValidator : AbstractValidator<UploadVideoRequest>
{
    // Note: .mov files are excluded because browsers have limited codec support for QuickTime format
    // MP4 and WebM are the most widely supported formats for HTML5 video playback
    public static IReadOnlyList<string> AllowedFileExtensions { get; } = [".mp4", ".webm", ".avi", ".wmv"];
  
    public UploadVideoRequestValidator()
    {
        RuleFor(x => x.FileExtension)
            .Must(ext => AllowedFileExtensions.Contains(ext))
            .WithMessage($"Unsupported file type. Allowed types are: {string.Join(", ", AllowedFileExtensions)}. Note: .mov files are not supported due to browser compatibility issues.");
    }
}