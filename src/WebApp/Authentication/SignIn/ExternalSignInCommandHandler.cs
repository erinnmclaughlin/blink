using Blink.WebApp.Components.Account.Pages;
using Blink.WebApp.Data;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;

namespace Blink.WebApp.Authentication.SignIn;

internal sealed class ExternalSignInCommandHandler : IRequestHandler<ExternalSignInCommand>
{
    private readonly HttpContext _httpContext;
    private readonly SignInManager<BlinkUser> _signInManager;
    
    public ExternalSignInCommandHandler(IHttpContextAccessor httpContextAccessor, SignInManager<BlinkUser> signInManager)
    {
        _httpContext = httpContextAccessor.HttpContext!;
        _signInManager = signInManager;
    }

    public async Task Handle(ExternalSignInCommand request, CancellationToken cancellationToken)
    {
        var redirectUrl = BuildRedirectUrl(request);
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(request.Provider, redirectUrl);
        await _httpContext.ChallengeAsync(request.Provider, properties);
    }

    private string BuildRedirectUrl(ExternalSignInCommand request)
    {
        var queryString = new QueryString()
            .Add("returnUrl", request.ReturnUrl)
            .Add("action", ExternalLogin.LoginCallbackAction);

        return UriHelper.BuildRelative(_httpContext.Request.PathBase, "/account/externalLogin", queryString);
    }
}