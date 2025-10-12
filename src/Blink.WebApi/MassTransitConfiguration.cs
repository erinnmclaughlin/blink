using Blink.WebApi.Videos.Thumbnails;
using MassTransit;

namespace Blink.WebApi;

public static class MassTransitConfiguration
{
    public static void AddAndConfigureServiceBus(this WebApplicationBuilder builder)
    {
        builder.Services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.AddConsumer<VideoThumbnailGeneratedConsumer>();
            busConfigurator.AddConsumer<VideoUploadedEventConsumer>();

            busConfigurator.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(builder.Configuration.GetConnectionString(ServiceNames.Messaging));

                configurator.ConfigureEndpoints(context);
            });
        });
    }
}
