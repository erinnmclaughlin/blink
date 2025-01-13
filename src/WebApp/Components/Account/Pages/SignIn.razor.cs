using Blink.WebApp.Authentication.SignIn;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;

namespace Blink.WebApp.Components.Account.Pages;

public sealed partial class SignIn
{
    private RequestStatus RequestStatus { get; set; }

    [CascadingParameter]
    private HttpContext HttpContext { get; set; } = default!;

    [SupplyParameterFromForm]
    private SignInCommand Input { get; set; } = new();

    [SupplyParameterFromQuery]
    private string? ReturnUrl { get; set; }

    protected override async Task OnInitializedAsync()
    {
        if (HttpMethods.IsGet(HttpContext.Request.Method))
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
        }
    }

    public async Task LoginUser()
    {
        RequestStatus = RequestStatus.Sending;
        StateHasChanged();

        await Mediator.Send(Input);
        RequestStatus = RequestStatus.Sent;
    }

    public sealed class FormValidator : AbstractValidator<SignIn>
    {
        public FormValidator(IValidator<SignInCommand> commandValidator)
        {
            RuleFor(x => x.Input).SetValidator(commandValidator);
        }
    }
}