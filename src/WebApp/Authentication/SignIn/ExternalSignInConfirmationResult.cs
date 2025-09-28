using Blink.WebApp.Data;
using Microsoft.AspNetCore.Identity;

namespace Blink.WebApp.Authentication.SignIn;

public sealed record ExternalSignInConfirmationResult(IdentityResult Result, BlinkUser User);
