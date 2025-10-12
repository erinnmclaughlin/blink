using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

namespace Blink.AppHost;

// TODO: Use Azure Service Bus instead of In-Memory for MassTransit
public static class ServiceBusDefaults
{
    public static AzureServiceBusResources AddAndConfigureServiceBus(this IDistributedApplicationBuilder builder)
    {
        var serviceBus = builder
            .AddAzureServiceBus(ServiceNames.ServiceBus)
            .WithExternalHttpEndpoints();

        if (builder.Environment.IsDevelopment())
        {
            serviceBus.RunAsEmulator();
        }

        var videosTopic = serviceBus.AddServiceBusTopic(ServiceNames.ServiceBusVideosTopic, ServiceNames.ServiceBusVideosTopic);
        videosTopic.AddServiceBusSubscription("sb-videos-subscription");

        return new AzureServiceBusResources
        {
            ServiceBus = serviceBus,
            VideosTopic = videosTopic
        };
    }
}

public sealed record AzureServiceBusResources
{
    public required IResourceBuilder<AzureServiceBusResource> ServiceBus { get; init; }

    public required IResourceBuilder<AzureServiceBusTopicResource> VideosTopic { get; init; }
}