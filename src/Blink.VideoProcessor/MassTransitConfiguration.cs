using Blink.VideoProcessor.Consumers;
using MassTransit;

namespace Blink.VideoProcessor;

public static class MassTransitConfiguration
{
    public static void AddAndConfigureMessaging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.AddConsumer<VideoMetadataProcessor>();
            busConfigurator.AddConsumer<VideoThumbnailGenerator>();

            busConfigurator.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(builder.Configuration.GetConnectionString(ServiceNames.Messaging));

                configurator.ConfigureEndpoints(context);
            });
        });
    }
}
