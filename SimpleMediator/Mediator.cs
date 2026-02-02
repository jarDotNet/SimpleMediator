using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Internals;
using System.Collections.Concurrent;
using System.Reflection;

namespace SimpleMediator;

public class Mediator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly MediatorConfiguration _config;
    private readonly ConcurrentDictionary<Type, RequestHandlerBase> _requestHandlerCache = new();
    private readonly ConcurrentDictionary<Type, EventPublisherWrapper> _eventPublisherCache = new();

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _config = serviceProvider.GetRequiredService<MediatorConfiguration>();
    }

    public async Task<TResponse> Send<TResponse>(object request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        var wrapper = _requestHandlerCache.GetOrAdd(requestType, type =>
            CreateWrapper<RequestHandlerBase>(typeof(RequestHandlerWrapperImpl<,>).MakeGenericType(type, typeof(TResponse))));

        var result = await wrapper.Execute(request, _serviceProvider, cancellationToken).ConfigureAwait(false);
        return (TResponse)result!;
    }

    public async Task Send(object command, CancellationToken cancellationToken = default)
    {
        await Send<Unit>(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task Publish(object @event, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = @event.GetType();

        var publisher = _eventPublisherCache.GetOrAdd(eventType, type =>
            CreateWrapper<EventPublisherWrapper>(typeof(EventPublisherWrapperImpl<>).MakeGenericType(type)));

        await publisher.Publish(@event, _serviceProvider, cancellationToken).ConfigureAwait(false);
    }

    public Task Publish<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        return Publish((object)@event, cancellationToken);
    }

    private T CreateWrapper<T>(Type wrapperType)
    {
        try
        {
            return (T)Activator.CreateInstance(wrapperType, _config)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }
    }
}
