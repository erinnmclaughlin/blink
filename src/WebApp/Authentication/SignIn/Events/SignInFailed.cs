using Blink.WebApp.Components.Account;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Blink.WebApp.Authentication.SignIn.Events;

public sealed record SignInFailedNotification(SignInResult Result) : INotification;

internal sealed class SignInFailedNotificationHandler : INotificationHandler<SignInFailedNotification>
{
    private readonly IdentityRedirectManager _redirectManager;

    public SignInFailedNotificationHandler(IdentityRedirectManager redirectManager)
    {
        _redirectManager = redirectManager;
    }

    public Task Handle(SignInFailedNotification notification, CancellationToken cancellationToken)
    {
        if (notification.Result.IsLockedOut)
        {
            _redirectManager.RedirectTo("account/lockout");
        }

        return Task.CompletedTask;
    }
}