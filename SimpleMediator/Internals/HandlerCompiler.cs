using System.Linq.Expressions;
using System.Reflection;

namespace SimpleMediator.Internals;

/// <summary>
/// Compiles handler methods into fast delegates using expression trees.
/// This avoids the overhead of MethodInfo.Invoke() on every call.
/// </summary>
internal static class HandlerCompiler
{
    /// <summary>
    /// Compiles a request handler method into a delegate.
    /// Supports handlers returning Task{T}, Task, or sync T.
    /// </summary>
    public static Func<object, TRequest, CancellationToken, Task<TResponse>> ForRequest<TRequest, TResponse>(
        Type handlerType,
        MethodInfo method)
    {
        var (handler, request, ct) = CreateParameters<TRequest>();
        var call = BuildMethodCall(handler, handlerType, method, request, ct);
        var body = NormalizeToTaskOfResponse<TResponse>(call, method.ReturnType);

        return Expression
            .Lambda<Func<object, TRequest, CancellationToken, Task<TResponse>>>(body, handler, request, ct)
            .Compile();
    }

    /// <summary>
    /// Compiles an event handler method into a delegate.
    /// Supports handlers returning Task or void.
    /// </summary>
    public static Func<object, TEvent, CancellationToken, Task?> ForEvent<TEvent>(
        Type handlerType,
        MethodInfo method)
    {
        var (handler, @event, ct) = CreateParameters<TEvent>();
        var call = BuildMethodCall(handler, handlerType, method, @event, ct);
        var body = NormalizeToNullableTask(call, method.ReturnType);

        return Expression
            .Lambda<Func<object, TEvent, CancellationToken, Task?>>(body, handler, @event, ct)
            .Compile();
    }

    private static (ParameterExpression handler, ParameterExpression arg, ParameterExpression ct)
        CreateParameters<TArg>()
    {
        return (
            Expression.Parameter(typeof(object), "handler"),
            Expression.Parameter(typeof(TArg), "arg"),
            Expression.Parameter(typeof(CancellationToken), "ct")
        );
    }

    private static Expression BuildMethodCall(
        ParameterExpression handler,
        Type handlerType,
        MethodInfo method,
        ParameterExpression arg,
        ParameterExpression ct)
    {
        var typedHandler = Expression.Convert(handler, handlerType);
        var hasCancellationToken = method.GetParameters().Length == 2;

        return hasCancellationToken
            ? Expression.Call(typedHandler, method, arg, ct)
            : Expression.Call(typedHandler, method, arg);
    }

    private static Expression NormalizeToTaskOfResponse<TResponse>(Expression call, Type returnType)
    {
        // Case 1: Already returns Task<TResponse>
        if (returnType == typeof(Task<TResponse>))
            return call;

        // Case 2: Returns Task (void handler) - wrap to return Task<TResponse>
        if (returnType == typeof(Task))
        {
            var method = typeof(HandlerCompiler)
                .GetMethod(nameof(AwaitAndReturnUnit), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeof(TResponse));
            return Expression.Call(method, call);
        }

        // Case 3: Returns Task<T> where T != TResponse - wrap with cast
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var innerType = returnType.GetGenericArguments()[0];
            var method = typeof(HandlerCompiler)
                .GetMethod(nameof(AwaitAndCast), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(typeof(TResponse), innerType);
            return Expression.Call(method, call);
        }

        // Case 4: Synchronous return - wrap in Task.FromResult
        var converted = Expression.Convert(call, typeof(TResponse));
        var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(typeof(TResponse));
        return Expression.Call(fromResult, converted);
    }

    private static Expression NormalizeToNullableTask(Expression call, Type returnType)
    {
        // Returns Task or Task<T> - just cast to Task
        if (typeof(Task).IsAssignableFrom(returnType))
            return Expression.Convert(call, typeof(Task));

        // Returns void - execute and return null
        return Expression.Block(call, Expression.Constant(null, typeof(Task)));
    }

    // Helper methods called by compiled delegates

    private static async Task<TResponse> AwaitAndReturnUnit<TResponse>(Task task)
    {
        await task.ConfigureAwait(false);
        return (TResponse)(object)Unit.Value;
    }

    private static async Task<TResponse> AwaitAndCast<TResponse, T>(Task<T> task)
    {
        var result = await task.ConfigureAwait(false);
        return (TResponse)(object)result!;
    }
}
