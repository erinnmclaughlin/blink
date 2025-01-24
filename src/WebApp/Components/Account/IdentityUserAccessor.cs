using Blink.WebApp.Data;
using Microsoft.AspNetCore.Identity;

namespace Blink.WebApp.Components.Account;

internal sealed class IdentityUserAccessor(UserManager<BlinkUser> userManager, IdentityRedirectManager redirectManager)
{
    public async Task<BlinkUser> GetRequiredUserAsync(HttpContext context)
    {
        var user = await userManager.GetUserAsync(context.User);

        if (user is null)
        {
            redirectManager.RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
        }

        return user;
    }
}
