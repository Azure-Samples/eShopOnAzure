using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using eShop.EventBus.Abstractions;
using eShop.EventBus.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace eShop.EventBusServiceBus;

public sealed class ServiceBusEventBus(
    ILogger<ServiceBusEventBus> logger,
    IServiceProvider serviceProvider,
    ServiceBusClient client,
    IOptions<EventBusSubscriptionInfo> subscriptionOptions,
    IOptions<EventBusOptions> options,
    ServiceBusAdministrationClient adminClient
    ) : IEventBus, IAsyncDisposable, IHostedService
{
    private const string TopicName = "eshop_event_bus";

    private static readonly JsonSerializerOptions s_indentedOptions = new() { WriteIndented = true };
    private static readonly JsonSerializerOptions s_caseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly EventBusSubscriptionInfo _subscriptionInfo = subscriptionOptions.Value;

    private ServiceBusSender _sender = null!;
    private ServiceBusProcessor? _processor;

    private TaskCompletionSource _connectionStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ValueTask DisposeAsync()
    {
        return new ValueTask(StopAsync(cancellationToken: default));
    }

    public async Task PublishAsync(IntegrationEvent @event)
    {
        await _connectionStarted.Task.WaitAsync(TimeSpan.FromSeconds(30));

        var routingKey = GetEventName(@event);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), s_indentedOptions);

        var message = new ServiceBusMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            Body = new BinaryData(body),
            Subject = routingKey,
        };

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Publishing event to ServiceBus: {EventId}", @event.Id);
        }

        // Retries automatically
        await _sender.SendMessageAsync(message);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting ServiceBus processing.");

        // Azure ServiceBus has a limit of 50 characters for rules. So we're removing "IntegrationEvent" from the type names
        // to keep the character count low. We'll replace the subscriptions in the dictionary at start so they don't have the "IntegrationEvent" suffix
        var tmp = _subscriptionInfo.EventTypes.ToList();
        foreach (var item in tmp)
        {
            if (item.Key.Contains("IntegrationEvent"))
            {
                _subscriptionInfo.EventTypes.Remove(item.Key);
                _subscriptionInfo.EventTypes.Add(item.Key.Replace("IntegrationEvent", ""), item.Value);
            }
        }

        try
        {
            if (string.IsNullOrEmpty(options.Value.SubscriptionClientName))
            {
                throw new InvalidOperationException($"EventBus {nameof(EventBusOptions.SubscriptionClientName)} must be set.");
            }

            _sender = client.CreateSender(queueOrTopicName: TopicName);
            _processor = client.CreateProcessor(topicName: TopicName, options.Value.SubscriptionClientName,
                new ServiceBusProcessorOptions() { AutoCompleteMessages = false });
            await using var ruleManager = client.CreateRuleManager(TopicName, options.Value.SubscriptionClientName);

            if (!await adminClient.SubscriptionExistsAsync(TopicName, options.Value.SubscriptionClientName))
            {
                try
                {
                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        logger.LogTrace("Creating subscription for ServiceBus: {Subscription}", options.Value.SubscriptionClientName);
                    }

                    await adminClient.CreateSubscriptionAsync(TopicName, options.Value.SubscriptionClientName);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
                {
                    logger.LogDebug("The subscription '{Subscription}' already exists.", options.Value.SubscriptionClientName);
                }
            }

            var currentRules = new List<string>();
            await foreach (var rule in ruleManager.GetRulesAsync())
            {
                currentRules.Add(rule.Name);
            }

            if (currentRules.Contains(RuleProperties.DefaultRuleName))
            {
                try
                {
                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        logger.LogTrace("Deleting default rule for ServiceBus subscription: {Subscription}", options.Value.SubscriptionClientName);
                    }

                    await ruleManager.DeleteRuleAsync(RuleProperties.DefaultRuleName);
                }
                catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityNotFound) { }
            }

            foreach (var (eventName, _) in _subscriptionInfo.EventTypes)
            {
                if (!currentRules.Contains(eventName))
                {
                    try
                    {
                        if (logger.IsEnabled(LogLevel.Trace))
                        {
                            logger.LogTrace("Creating rule for ServiceBus: {EventName}", eventName);
                        }
                        await ruleManager.CreateRuleAsync(new CreateRuleOptions
                        {
                            Filter = new CorrelationRuleFilter() { Subject = eventName },
                            Name = eventName
                        });
                    }
                    catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
                    {
                        logger.LogDebug("The messaging entity rule for '{EventName}' already exists.", eventName);
                    }
                }
            }

            _processor.ProcessMessageAsync += OnMessageReceived;
            _processor.ProcessErrorAsync += (ProcessErrorEventArgs error) =>
            {
                logger.LogError(error.Exception, "Error with ServiceBus processor. {ErrorSource}", error.ErrorSource);
                return Task.CompletedTask;
            };

            await _processor.StartProcessingAsync(cancellationToken);

            _connectionStarted.TrySetResult();

            logger.LogInformation("ServiceBus started processing successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ServiceBus failed to start processing.");
            _connectionStarted.TrySetException(ex);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _connectionStarted.TrySetCanceled();

        if (_sender is null)
        {
            // StartAsync never ran, or it failed before creating sender and processor
            return;
        }

        await _sender.DisposeAsync();

        if (_processor is null)
        {
            return;
        }

        await _processor.DisposeAsync();
    }

    private async Task OnMessageReceived(ProcessMessageEventArgs eventArgs)
    {
        var eventName = eventArgs.Message.Subject;
        var message = eventArgs.Message.Body.ToString();

        try
        {
            await ProcessEvent(eventName, message);

            // Tell ServiceBus that we've handled the message so it will be removed from the Topic
            await eventArgs.CompleteMessageAsync(eventArgs.Message);
        }
        catch (Exception ex)
        {
            // On Exception, in a REAL WORLD app the event should be handled by either
            // Abandoning the message so another receiver can process the item ASAP
            // or DeadLetter the message so the message won't be received by another processor
            // and can be seen by a receiver scoped to the dead letter queue

            // Currently the message will stay on the Topic on error and another processor will be able to pick up the message for processing
            // once this message received callback completes (due to using PeekLock message processing).
            // If the message is continuously received but not completed then it will end up on the dead letter queue after hitting the
            // Max delivery count (default 10).
            // See https://learn.microsoft.com/azure/service-bus-messaging/message-transfers-locks-settlement#peeklock for more details.
            logger.LogWarning(ex, "Error Processing message \"{Message}\"", message);
        }
    }

    private async Task ProcessEvent(string eventName, string message)
    {
        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("Processing ServiceBus event: {EventName}", eventName);
        }

        await using var scope = serviceProvider.CreateAsyncScope();

        if (!_subscriptionInfo.EventTypes.TryGetValue(eventName, out var eventType))
        {
            logger.LogWarning("Unable to resolve event type for event name {EventName}", eventName);
            return;
        }

        // Deserialize the event
        var integrationEvent = JsonSerializer.Deserialize(message, eventType, s_caseInsensitiveOptions) as IntegrationEvent;

        foreach (var handler in scope.ServiceProvider.GetKeyedServices<IIntegrationEventHandler>(eventType))
        {
            await handler.Handle(integrationEvent);
        }
    }

    private string GetEventName(IntegrationEvent @event)
    {
        // These type names might be longer than 50 characters which isn't supported by servicebus rules.
        // Let's remove the IntegrationEvent suffix to keep the rule names small
        return @event.GetType().Name.Replace("IntegrationEvent", "");
    }
}
