using Blink.Web.Client;
using Microsoft.FeatureManagement;

namespace Blink.Web;

public sealed class FeatureFlagManager : IFeatureFlagManager
{
    private readonly IFeatureManager _featureManager;

    public FeatureFlagManager(IFeatureManager featureManager)
    {
        _featureManager = featureManager;
    }

    public Task<bool> IsEnabledAsync(string featureFlagName)
    {
        return _featureManager.IsEnabledAsync(featureFlagName);
    }
}