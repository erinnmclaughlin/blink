using MediatR;

namespace Blink.WebApp.Authentication.SignIn;

public sealed class ExternalSignInCommand : IRequest
{
    public string Provider { get; set; } = "";
    public string ReturnUrl { get; set; } = "";
}