using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

namespace Blink.AppHost;

public static class AzureStorageDefaults
{
    public static AzureStorageResources AddAndConfigureAzureStorage(this IDistributedApplicationBuilder builder)
    {
        var storage = builder.AddAzureStorage("storage");

        if (builder.Environment.IsDevelopment())
        {
            storage.RunAsEmulator(azurite => azurite.WithDataVolume());
        }

        return new AzureStorageResources
        {
            Blobs = storage.AddBlobs("blobs"),
            Queues = storage.AddQueues("queues")
        };
    }
}

public sealed record AzureStorageResources
{
    public required IResourceBuilder<AzureBlobStorageResource> Blobs { get; init; }
    public required IResourceBuilder<AzureQueueStorageResource> Queues { get; init; }
}
