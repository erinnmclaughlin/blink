using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

namespace Blink.AppHost;

public static class ServiceBusDefaults
{
    public static AzureServiceBusResources AddAndConfigureServiceBus(this IDistributedApplicationBuilder builder)
    {
        var serviceBus = builder.AddAzureServiceBus(ServiceNames.ServiceBus);

        if (builder.Environment.IsDevelopment())
        {
            serviceBus.RunAsEmulator();
        }

        return new AzureServiceBusResources
        {
            ServiceBus = serviceBus,
            VideosTopic = serviceBus.AddServiceBusTopic(ServiceNames.ServiceBusVideosTopic),
        };
    }
}

public sealed record AzureServiceBusResources
{
    public required IResourceBuilder<AzureServiceBusResource> ServiceBus { get; init; }

    public required IResourceBuilder<AzureServiceBusTopicResource> VideosTopic { get; init; }
}