using MediatR;

namespace Blink.WebApp.Authentication.SignIn;

public sealed class SignInCommand : IRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}
