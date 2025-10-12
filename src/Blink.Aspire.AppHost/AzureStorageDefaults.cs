using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

namespace Blink.AppHost;

public static class AzureStorageDefaults
{
    public static AzureStorageResources AddAndConfigureAzureStorage(this IDistributedApplicationBuilder builder)
    {
        var storage = builder.AddAzureStorage(ServiceNames.AzureStorage);

        if (builder.Environment.IsDevelopment())
        {
            storage.RunAsEmulator(azurite => azurite.WithDataVolume());
        }

        return new AzureStorageResources
        {
            Blobs = storage.AddBlobs(ServiceNames.AzureBlobStorage)
        };
    }
}

public sealed record AzureStorageResources
{
    public required IResourceBuilder<AzureBlobStorageResource> Blobs { get; init; }
}
