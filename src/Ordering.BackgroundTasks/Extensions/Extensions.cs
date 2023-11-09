using System.Text.Json.Serialization;
using eShop.Ordering.BackgroundTasks.Events;
using eShop.EventBusServiceBus;

namespace eShop.Ordering.BackgroundTasks.Extensions;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        builder.AddServiceBusEventBus("EventBus")
               .ConfigureJsonOptions(options => options.TypeInfoResolverChain.Add(IntegrationEventContext.Default));

        builder.AddNpgsqlDataSource("OrderingDB");

        builder.Services.AddOptions<BackgroundTaskOptions>()
            .BindConfiguration(nameof(BackgroundTaskOptions));

        builder.Services.AddHostedService<GracePeriodManagerService>();
    }
}

[JsonSerializable(typeof(GracePeriodConfirmedIntegrationEvent))]
partial class IntegrationEventContext : JsonSerializerContext
{

}
