using Blink.WebApp.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Blink.WebApp.Components.Account.Pages;
public sealed partial class ExternalLogin
{
    public const string LoginCallbackAction = "LoginCallback";

    private ExternalLoginInfo? ExternalLoginInfo { get; set; }
    private string? Message { get; set; }
    private RequestStatus RequestStatus { get; set; }

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private InputModel Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? RemoteError { get; set; }

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    [SupplyParameterFromQuery]
    private string? Action { get; set; }

    private string? ProviderDisplayName => ExternalLoginInfo?.ProviderDisplayName;

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

        var user = new BlinkUser();

        await UserEmailStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
        await UserEmailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

        var result = await UserManager.CreateAsync(user);
        if (result.Succeeded)
        {
            result = await UserManager.AddLoginAsync(user, ExternalLoginInfo);
            if (result.Succeeded)
            {
                Logger.LogInformation("User created an account using {Name} provider.", ExternalLoginInfo.LoginProvider);

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
        }

        Message = $"Error: {string.Join(",", result.Errors.Select(error => error.Description))}";
        RequestStatus = RequestStatus.Sent;
    }

    private sealed class InputModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = "";
    }
}