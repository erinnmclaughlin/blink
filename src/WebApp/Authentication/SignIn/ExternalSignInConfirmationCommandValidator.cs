using FluentValidation;

namespace Blink.WebApp.Authentication.SignIn;

public sealed class ExternalSignInConfirmationCommandValidator : AbstractValidator<ExternalSignInConfirmationCommand>
{
    public ExternalSignInConfirmationCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.LoginProvider).NotEmpty();
        RuleFor(x => x.ProviderKey).NotEmpty();
    }
}