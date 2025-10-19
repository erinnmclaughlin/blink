using Microsoft.AspNetCore.Components;

namespace Blink.Web.Authentication.Components;

public sealed partial class AppAuthorizeView
{
    [Parameter]
    public RenderFragment<CurrentUser>? ChildContent { get; set; }

    [Parameter]
    public RenderFragment<CurrentUser>? Authorized { get; set; }

    [Parameter]
    public RenderFragment? NotAuthorized { get; set; }

    protected override void OnParametersSet()
    {
        if (ChildContent is not null && (Authorized is not null || NotAuthorized is not null))
        {
            throw new InvalidOperationException($"Cannot set both {nameof(ChildContent)} and {nameof(Authorized)}/{nameof(NotAuthorized)}.");
        }
    }
}
