using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMediator.Tests;

/// <summary>
/// Tests for advanced pipeline middleware scenarios.
/// </summary>
public class PipelineMiddlewareTests : MediatorTestBase
{
    [Fact]
    public async Task PipelineMiddleware_CanModifyRequest()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<ModifiableQueryHandler>();
        services.AddMediatorPipelineMiddleware<ModifiableQuery, string, RequestModifyingMiddleware>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        var query = new ModifiableQuery { Value = 10 };
        var result = await mediator.Send<string>(query);

        result.Should().Be("Modified: 20"); // Middleware doubles the value
    }

    [Fact]
    public async Task PipelineMiddleware_CanShortCircuit()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<ShortCircuitQueryHandler>();
        services.AddMediatorPipelineMiddleware<ShortCircuitQuery, string, ShortCircuitMiddleware>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        ShortCircuitQueryHandler.WasCalled = false;
        var query = new ShortCircuitQuery { ShouldShortCircuit = true };

        var result = await mediator.Send<string>(query);

        result.Should().Be("Short-circuited");
        ShortCircuitQueryHandler.WasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task PipelineMiddleware_CanAccessCancellationToken()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TokenQueryHandler>();
        services.AddMediatorPipelineMiddleware<TokenQuery, bool, CancellationCheckingMiddleware>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        using var cts = new CancellationTokenSource();
        var query = new TokenQuery();

        var result = await mediator.Send<bool>(query, cts.Token);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task PipelineMiddleware_CanHandleExceptions()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<ExceptionQueryHandler>();
        services.AddMediatorPipelineMiddleware<ExceptionQuery, string, ExceptionHandlingMiddleware>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        var query = new ExceptionQuery { ThrowException = true };
        var result = await mediator.Send<string>(query);

        result.Should().Be("Exception handled");
    }

    [Fact]
    public async Task PipelineMiddleware_ExecutesInRegistrationOrder()
    {
        var (services, _) = CreateManualSetup();
        var tracker = new PipelineTracker();
        services.AddSingleton(tracker);
        services.AddTransient<OrderTestQueryHandler>();
        services.AddMediatorPipelineMiddleware<OrderTestQuery, string, OrderTestMiddleware1>();
        services.AddMediatorPipelineMiddleware<OrderTestQuery, string, OrderTestMiddleware2>();
        services.AddMediatorPipelineMiddleware<OrderTestQuery, string, OrderTestMiddleware3>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        await mediator.Send<string>(new OrderTestQuery());

        tracker.Log.Should().Equal(
            "Middleware1-Before",
            "Middleware2-Before",
            "Middleware3-Before",
            "Handler",
            "Middleware3-After",
            "Middleware2-After",
            "Middleware1-After");
    }

    [Fact]
    public async Task OpenGenericPipelineMiddleware_AppliesIndependentlyToEachRequestType()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<Query1Handler>();
        services.AddTransient<Query2Handler>();
        services.AddMediatorPipelineMiddleware(typeof(UniversalLoggingMiddleware<,>));
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        UniversalLoggingMiddleware<Query1, string>.CallCount = 0;
        UniversalLoggingMiddleware<Query2, int>.CallCount = 0;

        await mediator.Send<string>(new Query1());
        await mediator.Send<int>(new Query2());

        UniversalLoggingMiddleware<Query1, string>.CallCount.Should().Be(1);
        UniversalLoggingMiddleware<Query2, int>.CallCount.Should().Be(1);
    }
}

public class ModifiableQuery
{
    public int Value { get; set; }
}

public class ModifiableQueryHandler
{
    public Task<string> Handle(ModifiableQuery query)
    {
        return Task.FromResult($"Modified: {query.Value}");
    }
}

public class RequestModifyingMiddleware : IPipelineMiddleware<ModifiableQuery, string>
{
    public async Task<string> Handle(ModifiableQuery request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        request.Value *= 2;
        return await next(cancellationToken);
    }
}

public class ShortCircuitQuery
{
    public bool ShouldShortCircuit { get; set; }
}

public class ShortCircuitQueryHandler
{
    public static bool WasCalled { get; set; }

    public Task<string> Handle(ShortCircuitQuery query)
    {
        WasCalled = true;
        return Task.FromResult("Handler result");
    }
}

public class ShortCircuitMiddleware : IPipelineMiddleware<ShortCircuitQuery, string>
{
    public async Task<string> Handle(ShortCircuitQuery request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        if (request.ShouldShortCircuit)
        {
            return "Short-circuited";
        }
        return await next(cancellationToken);
    }
}

public class TokenQuery { }

public class TokenQueryHandler
{
    public Task<bool> Handle(TokenQuery query, CancellationToken cancellationToken)
    {
        return Task.FromResult(!cancellationToken.IsCancellationRequested);
    }
}

public class CancellationCheckingMiddleware : IPipelineMiddleware<TokenQuery, bool>
{
    public async Task<bool> Handle(TokenQuery request, RequestHandlerDelegate<bool> next, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        return await next(cancellationToken);
    }
}

public class ExceptionQuery
{
    public bool ThrowException { get; set; }
}

public class ExceptionQueryHandler
{
    public Task<string> Handle(ExceptionQuery query)
    {
        if (query.ThrowException)
        {
            throw new InvalidOperationException("Test exception");
        }
        return Task.FromResult("Success");
    }
}

public class ExceptionHandlingMiddleware : IPipelineMiddleware<ExceptionQuery, string>
{
    public async Task<string> Handle(ExceptionQuery request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return "Exception handled";
        }
    }
}

public class OrderTestQuery { }

public class OrderTestQueryHandler
{
    private readonly PipelineTracker _tracker;

    public OrderTestQueryHandler(PipelineTracker tracker) => _tracker = tracker;

    public Task<string> Handle(OrderTestQuery query)
    {
        _tracker.Log.Add("Handler");
        return Task.FromResult("Result");
    }
}

public class OrderTestMiddleware1 : IPipelineMiddleware<OrderTestQuery, string>
{
    private readonly PipelineTracker _tracker;

    public OrderTestMiddleware1(PipelineTracker tracker) => _tracker = tracker;

    public async Task<string> Handle(OrderTestQuery request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        _tracker.Log.Add("Middleware1-Before");
        var result = await next(cancellationToken);
        _tracker.Log.Add("Middleware1-After");
        return result;
    }
}

public class OrderTestMiddleware2 : IPipelineMiddleware<OrderTestQuery, string>
{
    private readonly PipelineTracker _tracker;

    public OrderTestMiddleware2(PipelineTracker tracker) => _tracker = tracker;

    public async Task<string> Handle(OrderTestQuery request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        _tracker.Log.Add("Middleware2-Before");
        var result = await next(cancellationToken);
        _tracker.Log.Add("Middleware2-After");
        return result;
    }
}

public class OrderTestMiddleware3 : IPipelineMiddleware<OrderTestQuery, string>
{
    private readonly PipelineTracker _tracker;

    public OrderTestMiddleware3(PipelineTracker tracker) => _tracker = tracker;

    public async Task<string> Handle(OrderTestQuery request, RequestHandlerDelegate<string> next, CancellationToken cancellationToken)
    {
        _tracker.Log.Add("Middleware3-Before");
        var result = await next(cancellationToken);
        _tracker.Log.Add("Middleware3-After");
        return result;
    }
}

public class Query1 { }

public class Query1Handler
{
    public Task<string> Handle(Query1 query) => Task.FromResult("Query1");
}

public class Query2 { }

public class Query2Handler
{
    public Task<int> Handle(Query2 query) => Task.FromResult(42);
}

public class UniversalLoggingMiddleware<TRequest, TResponse> : IPipelineMiddleware<TRequest, TResponse>
    where TRequest : notnull
{
    public static int CallCount { get; set; }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        CallCount++;
        return await next(cancellationToken);
    }
}
