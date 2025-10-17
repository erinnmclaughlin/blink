using Blink.VideoSummarizer.Consumers;
using MassTransit;

namespace Blink.VideoSummarizer;

public static class MassTransitConfiguration
{
    public static void AddAndConfigureMessaging(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.AddConsumer<VideoSummaryGenerator>();

            busConfigurator.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(builder.Configuration.GetConnectionString(ServiceNames.Messaging));

                configurator.ConfigureEndpoints(context);
            });
        });
    }
}

