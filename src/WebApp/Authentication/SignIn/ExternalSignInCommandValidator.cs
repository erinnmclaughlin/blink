using FluentValidation;

namespace Blink.WebApp.Authentication.SignIn;

public sealed class ExternalSignInCommandValidator : AbstractValidator<ExternalSignInCommand>
{
    public ExternalSignInCommandValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().WithMessage("Provider cannot be empty");
    }
}