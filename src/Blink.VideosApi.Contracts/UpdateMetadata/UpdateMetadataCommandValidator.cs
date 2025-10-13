namespace Blink.VideosApi.Contracts.UpdateMetadata;

public sealed class UpdateMetadataCommandValidator : AbstractValidator<UpdateMetadataCommand>
{
    public UpdateMetadataCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required")
            .MaximumLength(512)
            .WithMessage("Title must not exceed 512 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .When(x => !string.IsNullOrEmpty(x.Description))
            .WithMessage("Description must not exceed 2000 characters");
    }
}

