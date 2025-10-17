using Blink.Web.Videos.Consumers;
using MassTransit;

namespace Blink.Web;

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
