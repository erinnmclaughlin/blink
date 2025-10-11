using FluentValidation;

namespace Blink.WebApi.Videos.Upload;

public sealed class UploadVideoRequestValidator : AbstractValidator<UploadVideoRequest>
{
    public static IReadOnlyList<string> AllowedFileExtensions { get; } = [".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv"];
  
    public const long MaxFileSize = 500L * 1024 * 1024; // 500MB

    public UploadVideoRequestValidator()
    {
        RuleFor(x => x.FileSize)
            .GreaterThan(0)
            .WithMessage("File cannot be empty.");

        RuleFor(x => x.FileSize)
            .LessThanOrEqualTo(MaxFileSize)
            .WithMessage("File size exceeds maximum allowed size of 500MB.");

        RuleFor(x => x.FileExtension)
            .Must(ext => AllowedFileExtensions.Contains(ext))
            .WithMessage($"Unsupported file type. Allowed types are: {string.Join(", ", AllowedFileExtensions)}");
    }
}