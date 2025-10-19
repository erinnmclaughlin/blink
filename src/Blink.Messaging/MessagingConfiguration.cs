using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class MessagingConfiguration
{
    public static void AddBlinkMessaging<TAssemblyMarker>(this IHostApplicationBuilder builder, Action<IBusRegistrationConfigurator>? configure = null)
    {
        builder.Services.AddMassTransit(o =>
        {
            o.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(includeNamespace: true));

            o.AddConsumersFromNamespaceContaining<TAssemblyMarker>();
            
            o.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(builder.Configuration.GetConnectionString("blink-messaging"));

                configurator.ConfigureEndpoints(context);
            });
            
            configure?.Invoke(o);
        });
    }
    
    public static void AddBlinkMessaging(this IHostApplicationBuilder builder, Action<IBusRegistrationConfigurator>? configure = null)
    {
        builder.Services.AddMassTransit(o =>
        {
            o.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(includeNamespace: true));
            
            o.UsingRabbitMq((context, configurator) =>
            {
                configurator.Host(builder.Configuration.GetConnectionString("blink-messaging"));

                configurator.ConfigureEndpoints(context);
            });
            
            configure?.Invoke(o);
        });
    }
}
