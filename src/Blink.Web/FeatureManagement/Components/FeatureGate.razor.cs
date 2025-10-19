using Microsoft.AspNetCore.Components;
using Microsoft.FeatureManagement;

namespace Blink.Web.FeatureManagement.Components;

public sealed partial class FeatureGate
{
    private readonly IFeatureManager _featureManager;

    private bool IsEnabled { get; set; }
    
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    
    [Parameter, EditorRequired] 
    public required string FeatureName { get; set; }
    
    public FeatureGate(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    protected override async Task OnInitializedAsync()
    {
        IsEnabled = await _featureManager.IsEnabledAsync(FeatureName);
    }
}
