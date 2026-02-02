using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace SimpleMediator.Internals;

abstract class RequestHandlerBase
{
    public abstract Task<object?> Execute(object request, IServiceProvider sp, CancellationToken ct);
}

abstract class RequestHandlerWrapper<TRequest, TResponse> : RequestHandlerBase
    where TRequest : notnull
{
    public abstract Task<TResponse> Execute(TRequest request, IServiceProvider sp, CancellationToken ct);

    public sealed override async Task<object?> Execute(object request, IServiceProvider sp, CancellationToken ct)
        => await Execute((TRequest)request, sp, ct).ConfigureAwait(false);
}

internal class RequestHandlerWrapperImpl<TRequest, TResponse> : RequestHandlerWrapper<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly HandlerMetadata _handler;

    private sealed record HandlerMetadata(Type HandlerType, Func<object, TRequest, CancellationToken, Task<TResponse>> Handle);

    public RequestHandlerWrapperImpl(MediatorConfiguration config)
    {
        _handler = DiscoverHandler(config);
    }

    public override async Task<TResponse> Execute(TRequest request, IServiceProvider sp, CancellationToken ct)
    {
        var handlerInstance = sp.GetRequiredService(_handler.HandlerType);

        var pipeline = BuildPipeline(request, handlerInstance, _handler, sp);
        return await pipeline(ct).ConfigureAwait(false);
    }

    private static RequestHandlerDelegate<TResponse> BuildPipeline(
        TRequest request,
        object handlerInstance,
        HandlerMetadata handler,
        IServiceProvider sp)
    {
        Task<TResponse> delegateHandler(CancellationToken ct = default) => handler.Handle(handlerInstance, request, ct);

        var pipelineMiddlewares = sp.GetServices<IPipelineMiddleware<TRequest, TResponse>>().Reverse();
        
        return pipelineMiddlewares.Aggregate(
            (RequestHandlerDelegate<TResponse>)delegateHandler,
            (next, middleware) => ct => middleware.Handle(request, next, ct)
        );
    }

    private static HandlerMetadata DiscoverHandler(MediatorConfiguration config)
    {
        var requestType = typeof(TRequest);
        var handlerName = $"{requestType.FullName}Handler";

        var handlerType = config.HandlerAssemblies
            .Select(a => a.GetType(handlerName))
            .FirstOrDefault(t => t != null)
            ?? throw new InvalidOperationException($"No handler found for {requestType.Name} in any registered assembly.");

        var method = handlerType.GetMethod("Handle", [requestType, typeof(CancellationToken)])
            ?? handlerType.GetMethod("Handle", [requestType])
            ?? throw new InvalidOperationException($"Handler {handlerType.Name} must have a Handle method.");

        ValidateReturnType(requestType, method);

        var handler = HandlerCompiler.ForRequest<TRequest, TResponse>(handlerType, method);
        return new HandlerMetadata(handlerType, handler);
    }

    private static void ValidateReturnType(Type requestType, MethodInfo method)
    {
        var returnType = method.ReturnType;

        var actualReturnType = returnType switch
        {
            var t when t == typeof(Task) => typeof(Unit),
            var t when t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Task<>) => t.GetGenericArguments()[0],
            _ => returnType
        };

        if (!typeof(TResponse).IsAssignableFrom(actualReturnType))
        {
            throw new InvalidOperationException(
                $"Handler for {requestType.Name} returns {actualReturnType.Name}, but {typeof(TResponse).Name} was expected.");
        }
    }
}
