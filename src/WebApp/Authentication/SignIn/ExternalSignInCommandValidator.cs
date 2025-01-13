using FluentValidation;

namespace Blink.WebApp.Authentication.SignIn;

public class ExternalSignInCommandValidator : AbstractValidator<ExternalSignInCommand>
{
    public ExternalSignInCommandValidator()
    {
        RuleFor(x => x.Provider).NotEmpty().WithMessage("Provider cannot be empty");
    }
}