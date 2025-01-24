using Blink.WebApp.Components.Account;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace Blink.WebApp.Authentication.SignIn.Events;

public sealed record SignInSucceededNotification : INotification;

internal sealed class SignInSucceededNotificationHandler : INotificationHandler<SignInSucceededNotification>
{
    private readonly NavigationManager _navigationManager;
    private readonly IdentityRedirectManager _redirectManager;

    public SignInSucceededNotificationHandler(NavigationManager navigationManager, IdentityRedirectManager redirectManager)
    {
        _navigationManager = navigationManager;
        _redirectManager = redirectManager;
    }

    public Task Handle(SignInSucceededNotification notification, CancellationToken cancellationToken)
    {
        var queryString = _navigationManager.ToAbsoluteUri(_navigationManager.Uri).Query;
        var returnUrl = QueryHelpers.ParseNullableQuery(queryString)?.GetValueOrDefault("returnUrl");

        _redirectManager.RedirectTo(returnUrl);

        return Task.CompletedTask;
    }
}
