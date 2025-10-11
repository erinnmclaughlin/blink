using FluentValidation;

namespace Blink.WebApi.Videos.Upload;

public sealed class UploadVideoRequestValidator : AbstractValidator<UploadVideoRequest>
{
    public static IReadOnlyList<string> AllowedFileExtensions { get; } = [".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv"];
  
    public UploadVideoRequestValidator()
    {
        RuleFor(x => x.FileExtension)
            .Must(ext => AllowedFileExtensions.Contains(ext))
            .WithMessage($"Unsupported file type. Allowed types are: {string.Join(", ", AllowedFileExtensions)}");
    }
}