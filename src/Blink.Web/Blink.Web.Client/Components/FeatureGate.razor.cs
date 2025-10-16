using Microsoft.AspNetCore.Components;

namespace Blink.Web.Client.Components;

public sealed partial class FeatureGate
{
    private readonly IFeatureFlagManager _featureFlagManager;

    private bool IsEnabled { get; set; }
    
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
    
    [Parameter, EditorRequired] 
    public required string FeatureName { get; set; }
    
    public FeatureGate(IFeatureFlagManager featureFlagManager)
    {
        _featureFlagManager = featureFlagManager;
    }

    protected override async Task OnInitializedAsync()
    {
        IsEnabled = await _featureFlagManager.IsEnabledAsync(FeatureName);
    }
}