using eShop.EventBus.Abstractions;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace eShop.EventBusServiceBus;

public static class ServiceBusDependencyInjectionExtensions
{
    // {
    //   "EventBus": {
    //     "SubscriptionClientName": "..."
    //   }
    // }

    private const string SectionName = "EventBus";

    public static IEventBusBuilder AddServiceBusEventBus(this IHostApplicationBuilder builder, string connectionName)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddAzureServiceBusClient(connectionName, o =>
        {
            o.DisableTracing = false;
            o.HealthCheckTopicName = "eshop_event_bus";

            // When using the namespace instead of a full connection string, Service Bus will attempt to connect with your Azure credentials
            // Make sure that you have the "Azure Service Bus Data Owner" role so that you can create the subscription rules,
            // as well as receive and send messages.
        });

        // Temporary until https://github.com/dotnet/aspire/issues/431
        var connectionString = builder.Configuration.GetConnectionString(connectionName);
        builder.Services.AddAzureClients(o =>
        {
            if (!string.IsNullOrEmpty(connectionString))
            {
                // a service bus namespace can't contain ';'. if it is found assume it is a connection string
                if (!connectionString.Contains(';'))
                {
                    o.AddServiceBusAdministrationClientWithNamespace(connectionString);
                }
                else
                {
                    o.AddServiceBusAdministrationClient(connectionString);
                }
            }
        });

        // Options support
        builder.Services.Configure<EventBusOptions>(builder.Configuration.GetSection(SectionName));

        builder.Services.AddSingleton<IEventBus, ServiceBusEventBus>();
        // Start consuming messages as soon as the application starts
        builder.Services.AddSingleton<IHostedService>(sp => (ServiceBusEventBus)sp.GetRequiredService<IEventBus>());

        return new EventBusBuilder(builder.Services);
    }

    private sealed class EventBusBuilder(IServiceCollection services) : IEventBusBuilder
    {
        public IServiceCollection Services => services;
    }
}
