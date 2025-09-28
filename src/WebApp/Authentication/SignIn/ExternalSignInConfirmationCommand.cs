using MediatR;

namespace Blink.WebApp.Authentication.SignIn;

public sealed class ExternalSignInConfirmationCommand : IRequest<ExternalSignInConfirmationResult>
{
    public string Email { get; set; } = "";
    public string LoginProvider { get; set; } = "";
    public string ProviderKey { get; set; } = "";
}
