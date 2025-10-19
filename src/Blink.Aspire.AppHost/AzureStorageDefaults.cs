using Aspire.Hosting.Azure;

namespace Blink.AppHost;

public static class AzureStorageDefaults
{
    public static AzureStorageResources AddAndConfigureAzureStorage(this IDistributedApplicationBuilder builder)
    {
        var storage = builder.AddAzureStorage("storage");

        if (builder.ExecutionContext.IsRunMode)
        {
            storage.RunAsEmulator(azurite => azurite.WithDataVolume());
        }

        return new AzureStorageResources
        {
            Blobs = storage.AddBlobs("blobs")
        };
    }
}

public sealed record AzureStorageResources
{
    public required IResourceBuilder<AzureBlobStorageResource> Blobs { get; init; }
}
