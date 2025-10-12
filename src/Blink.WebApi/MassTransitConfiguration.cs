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

            busConfigurator.AddConsumer<ThumbnailGenerationService>();

            //busConfigurator.UsingInMemory((context, config) =>
            //{
            //    config.ConfigureEndpoints(context);
            //});

            busConfigurator.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(builder.Configuration.GetConnectionString(ServiceNames.Messaging));

                configurator.ConfigureEndpoints(context);
            });
        });
    }
}
