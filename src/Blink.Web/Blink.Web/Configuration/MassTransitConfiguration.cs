using Blink.Web.EventConsumers;
using MassTransit;

namespace Blink.Web.Configuration;

public static class MassTransitConfiguration
{
    public static void AddAndConfigureServiceBus(this WebApplicationBuilder builder)
    {
        builder.Services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.AddConsumer<VideoMetadataExtractedConsumer>();
            busConfigurator.AddConsumer<VideoThumbnailGeneratedConsumer>();

            busConfigurator.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(builder.Configuration.GetConnectionString(ServiceNames.Messaging));

                configurator.ConfigureEndpoints(context);
            });
        });
    }
}
