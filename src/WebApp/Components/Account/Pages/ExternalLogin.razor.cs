using Blink.WebApp.Authentication.SignIn;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Blink.WebApp.Components.Account.Pages;
public sealed partial class ExternalLogin
{
    private readonly ISender _sender;

    public const string FormName = "external-login-confirmation";
    public const string LoginCallbackAction = "LoginCallback";

    private ExternalLoginInfo? ExternalLoginInfo { get; set; }
    private string? Message { get; set; }
    private RequestStatus RequestStatus { get; set; }

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private ExternalSignInConfirmationCommand Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? RemoteError { get; set; }

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery]
    private string? Action { get; set; }

    private string? ProviderDisplayName => ExternalLoginInfo?.ProviderDisplayName;

    public ExternalLogin(ISender sender)
    {
        _sender = sender;
    }

    protected override async Task OnInitializedAsync()
    {
        if (RemoteError is not null)
        {
            RedirectManager.RedirectToWithStatus("Account/Login", $"Error from external provider: {RemoteError}", HttpContext);
        }

        ExternalLoginInfo = await SignInManager.GetExternalLoginInfoAsync();
        if (ExternalLoginInfo is null)
        {
            RedirectManager.RedirectToWithStatus("Account/Login", "Error loading external login information.", HttpContext);
        }

        Input.LoginProvider = ExternalLoginInfo.LoginProvider;
        Input.ProviderKey = ExternalLoginInfo.ProviderKey;

        if (HttpMethods.IsGet(HttpContext.Request.Method))
        {
            if (Action == LoginCallbackAction)
            {
                await OnLoginCallbackAsync();
                return;
            }

            // We should only reach this page via the login callback, so redirect back to
            // the login page if we get here some other way.
            RedirectManager.RedirectTo("Account/Login");
        }
    }

    private async Task OnLoginCallbackAsync()
    {
        if (ExternalLoginInfo is null)
        {
            RedirectManager.RedirectToWithStatus("Account/Login", "Error loading external login information.", HttpContext);
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await SignInManager.ExternalLoginSignInAsync(
            ExternalLoginInfo.LoginProvider,
            ExternalLoginInfo.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (result.Succeeded)
        {
            Logger.LogInformation(
                "{Name} logged in with {LoginProvider} provider.",
                ExternalLoginInfo.Principal.Identity?.Name,
                ExternalLoginInfo.LoginProvider);
            RedirectManager.RedirectTo(ReturnUrl);
        }
        else if (result.IsLockedOut)
        {
            RedirectManager.RedirectTo("Account/Lockout");
        }

        // If the user does not have an account, then ask the user to create an account.
        if (ExternalLoginInfo.Principal.HasClaim(c => c.Type == ClaimTypes.Email))
        {
            Input.Email = ExternalLoginInfo.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
        }
    }

    private async Task OnValidSubmitAsync()
    {
        RequestStatus = RequestStatus.Sending;
        StateHasChanged();

        if (ExternalLoginInfo is null)
        {
            RedirectManager.RedirectToWithStatus("Account/Login", "Error loading external login information during confirmation.", HttpContext);
        }

        var (result, user) = await _sender.Send(Input);

        if (result.Succeeded)
        {
            var userId = await UserManager.GetUserIdAsync(user);
            var code = await UserManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

            var callbackUrl = NavigationManager.GetUriWithQueryParameters(
                NavigationManager.ToAbsoluteUri("Account/ConfirmEmail").AbsoluteUri,
                new Dictionary<string, object?> { ["userId"] = userId, ["code"] = code });

            await EmailSender.SendConfirmationLinkAsync(user, Input.Email, HtmlEncoder.Default.Encode(callbackUrl));

            // If account confirmation is required, we need to show the link if we don't have a real email sender
            if (UserManager.Options.SignIn.RequireConfirmedAccount)
            {
                RedirectManager.RedirectTo("Account/RegisterConfirmation", new() { ["email"] = Input.Email });
            }

            await SignInManager.SignInAsync(user, isPersistent: false, ExternalLoginInfo.LoginProvider);
            RedirectManager.RedirectTo(ReturnUrl);
        }

        Message = $"Error: {string.Join(",", result.Errors.Select(error => error.Description))}";
        RequestStatus = RequestStatus.Sent;
    }
}