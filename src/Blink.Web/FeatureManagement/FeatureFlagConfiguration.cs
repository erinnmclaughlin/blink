using Microsoft.FeatureManagement;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class FeatureFlagConfiguration
{
    public static void AddBlinkFeatureManagement(this IHostApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("features.json", optional: false, reloadOnChange: true);
        builder.Configuration.AddJsonFile($"features.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

        builder.Services.AddFeatureManagement();
        builder.Services.Configure<ConfigurationFeatureDefinitionProviderOptions>(o => o.CustomConfigurationMergingEnabled = true);
    }
}
