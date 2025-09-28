using Blink.WebApp.Data;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Blink.WebApp.Authentication.SignIn;

internal sealed class ExternalSignInConfirmationCommandHandler : IRequestHandler<ExternalSignInConfirmationCommand, ExternalSignInConfirmationResult>
{
    private readonly ILogger<ExternalSignInConfirmationCommandHandler> _logger;
    private readonly UserManager<BlinkUser> _userManager;

    public ExternalSignInConfirmationCommandHandler(ILogger<ExternalSignInConfirmationCommandHandler> logger, UserManager<BlinkUser> userManager)
    {
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<ExternalSignInConfirmationResult> Handle(ExternalSignInConfirmationCommand request, CancellationToken cancellationToken)
    {
        var user = new BlinkUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        cancellationToken.ThrowIfCancellationRequested();
        var result = await _userManager.CreateAsync(user);

        if (result.Succeeded)
        {
            var loginInfo = new ExternalLoginInfo(null!, request.LoginProvider, request.ProviderKey, null!);
            result = await _userManager.AddLoginAsync(user, loginInfo);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created an account using {Name} provider.", request.LoginProvider);
            }
        }

        return new ExternalSignInConfirmationResult(result, user);
    }
}
