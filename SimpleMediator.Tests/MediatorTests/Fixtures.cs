namespace SimpleMediator.Tests.MediatorTests;

/// <summary>
/// Test fixtures (queries, commands, events, handlers, middlewares) used by MediatorTests.
/// </summary>

public class TestQuery
{
    public int Value { get; set; }
}

public class TestQueryHandler
{
    public Task<string> Handle(TestQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Result: {query.Value}");
    }
}

public class TestCancellableQuery { }

public class TestCancellableQueryHandler
{
    public Task<bool> Handle(TestCancellableQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(!cancellationToken.IsCancellationRequested);
    }
}

public class SyncTestQuery
{
    public int Value { get; set; }
}

public class SyncTestQueryHandler
{
    public int Handle(SyncTestQuery query)
    {
        return query.Value;
    }
}

public class TestCommand
{
    public int Value { get; set; }
}

public class TestCommandHandler
{
    public Task Handle(TestCommand command, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class TestEvent
{
    public string Message { get; set; } = string.Empty;
}

public class TestEventHandler1
{
    public static int ExecutionCount { get; set; }

    public Task Handle(TestEvent @event)
    {
        ExecutionCount++;
        return Task.CompletedTask;
    }
}

public class TestEventHandler2
{
    public static int ExecutionCount { get; set; }

    public Task Handle(TestEvent @event, CancellationToken cancellationToken)
    {
        ExecutionCount++;
        return Task.CompletedTask;
    }
}

public class TestEventHandler3
{
    public static int ExecutionCount { get; set; }

    public Task Handle(TestEvent @event)
    {
        ExecutionCount++;
        return Task.CompletedTask;
    }
}

public class EventWithoutHandlers
{
    public string Data { get; set; } = string.Empty;
}

public class TestPipelineMiddleware : IPipelineMiddleware<TestQuery, string>
{
    private readonly PipelineTracker _tracker;

    public TestPipelineMiddleware(PipelineTracker tracker) => _tracker = tracker;

    public async Task<string> Handle(TestQuery request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        _tracker.Log.Add("Before");
        var result = await next(cancellationToken);
        _tracker.Log.Add("After");
        return result;
    }
}

public class TestPipelineMiddleware1 : IPipelineMiddleware<TestQuery, string>
{
    private readonly PipelineTracker _tracker;

    public TestPipelineMiddleware1(PipelineTracker tracker) => _tracker = tracker;

    public async Task<string> Handle(TestQuery request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        _tracker.Log.Add("Middleware1-Before");
        var result = await next(cancellationToken);
        _tracker.Log.Add("Middleware1-After");
        return result;
    }
}

public class TestPipelineMiddleware2 : IPipelineMiddleware<TestQuery, string>
{
    private readonly PipelineTracker _tracker;

    public TestPipelineMiddleware2(PipelineTracker tracker) => _tracker = tracker;

    public async Task<string> Handle(TestQuery request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        _tracker.Log.Add("Middleware2-Before");
        var result = await next(cancellationToken);
        _tracker.Log.Add("Middleware2-After");
        return result;
    }
}

public class GenericLoggingMiddleware<TRequest, TResponse> : IPipelineMiddleware<TRequest, TResponse>
    where TRequest : notnull
{
    public static bool WasCalled { get; set; }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        WasCalled = true;
        return await next(cancellationToken);
    }
}
