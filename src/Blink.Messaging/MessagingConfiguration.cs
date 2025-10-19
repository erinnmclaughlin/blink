using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingConfiguration
{
    public static void AddBlinkMessaging<T>(this T builder, Action<IBusRegistrationConfigurator>? configure = null) where T : IHostApplicationBuilder
    {
        builder.Services.AddMassTransit(busConfigurator =>
        {
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(builder.Configuration.GetConnectionString("blink-messaging"));

                configurator.ConfigureEndpoints(context);
            });
            
            configure?.Invoke(busConfigurator);
        });
    }
}
