namespace SimpleMediator.Internals;

abstract class EventPublisherWrapper
{
    public abstract Task Publish(object @event, IServiceProvider sp, CancellationToken ct);
}

internal class EventPublisherWrapperImpl<TEvent> : EventPublisherWrapper 
    where TEvent : notnull
{
    private readonly List<EventHandlerMetadata> _handlers;

    private sealed record EventHandlerMetadata(Type HandlerType, Func<object, TEvent, CancellationToken, Task?> Handle);

    public EventPublisherWrapperImpl(MediatorConfiguration config)
    {
        _handlers = DiscoverHandlers(config);
    }

    public override async Task Publish(object @event, IServiceProvider sp, CancellationToken ct)
    {
        var typedEvent = (TEvent)@event;
        var tasks = new List<Task>(_handlers.Count);

        foreach (var handler in _handlers)
        {
            var handlerInstance = sp.GetService(handler.HandlerType);
            if (handlerInstance is null) continue;

            var task = handler.Handle(handlerInstance, typedEvent, ct);
            if (task is not null)
            {
                tasks.Add(task);
            }
        }

        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    private static List<EventHandlerMetadata> DiscoverHandlers(MediatorConfiguration config)
    {
        var eventType = typeof(TEvent);
        var handlers = new List<EventHandlerMetadata>();

        foreach (var assembly in config.HandlerAssemblies)
        {
            foreach (var type in assembly.GetTypes().Where(t => !t.IsAbstract && !t.IsInterface))
            {
                var method = type.GetMethod("Handle", [eventType, typeof(CancellationToken)])
                          ?? type.GetMethod("Handle", [eventType]);

                if (method is not null)
                {
                    var handler = HandlerCompiler.ForEvent<TEvent>(type, method);
                    handlers.Add(new EventHandlerMetadata(type, handler));
                }
            }
        }

        return handlers;
    }
}
