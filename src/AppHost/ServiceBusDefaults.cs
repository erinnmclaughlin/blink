using Aspire.Hosting.Azure;
using Microsoft.Extensions.Hosting;

namespace Blink.AppHost;

public static class ServiceBusDefaults
{
    public static IResourceBuilder<AzureServiceBusResource> AddAndConfigureServiceBus(this IDistributedApplicationBuilder builder)
    {
        var serviceBus = builder.AddAzureServiceBus("messaging");

        if (builder.Environment.IsDevelopment())
        {
            serviceBus.RunAsEmulator();
        }

        return serviceBus;
    }
}
