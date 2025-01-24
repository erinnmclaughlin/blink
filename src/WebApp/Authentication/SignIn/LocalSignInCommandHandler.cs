using Blink.WebApp.Authentication.SignIn.Events;
using Blink.WebApp.Data;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Blink.WebApp.Authentication.SignIn;

internal sealed class LocalSignInCommandHandler : IRequestHandler<LocalSignInCommand>
{
    private readonly ILogger<LocalSignInCommandHandler> _logger;
    private readonly IMediator _mediator;
    private readonly SignInManager<BlinkUser> _signInManager;

    public LocalSignInCommandHandler(ILogger<LocalSignInCommandHandler> logger, IMediator mediator, SignInManager<BlinkUser> signInManager)
    {
        _logger = logger;
        _mediator = mediator;
        _signInManager = signInManager;
    }

    public async Task Handle(LocalSignInCommand request, CancellationToken cancellationToken)
    {
        var result = await _signInManager.PasswordSignInAsync(
            request.Email, 
            request.Password, 
            request.RememberMe,
            lockoutOnFailure: false
        );

        if (result.Succeeded)
        {
            await _mediator.Publish(new SignInSucceededNotification(), cancellationToken);
            return;
        }

        _logger.LogWarning("Failed to sign in user with email {Email}. Reason: {Result}", request.Email, result.ToString());
        await _mediator.Publish(new SignInFailedNotification(result), cancellationToken);
    }
}
