using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMediator.Tests.MediatorTests;

public class PipelineMiddlewareTests : MediatorTestBase
{
    [Fact]
    public async Task Send_WithPipelineMiddleware_ExecutesMiddlewareBeforeHandler()
    {
        var (services, _) = CreateManualSetup();
        var tracker = new PipelineTracker();
        services.AddSingleton(tracker);
        services.AddTransient<TestQueryHandler>();
        services.AddMediatorPipelineMiddleware<TestQuery, string, TestPipelineMiddleware>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        await mediator.Send<string>(new TestQuery { Value = 42 });

        tracker.Log.Should().Equal("Before", "After");
    }

    [Fact]
    public async Task Send_WithMultiplePipelineMiddlewares_ExecutesInCorrectOrder()
    {
        var (services, _) = CreateManualSetup();
        var tracker = new PipelineTracker();
        services.AddSingleton(tracker);
        services.AddTransient<TestQueryHandler>();
        services.AddMediatorPipelineMiddleware<TestQuery, string, TestPipelineMiddleware1>();
        services.AddMediatorPipelineMiddleware<TestQuery, string, TestPipelineMiddleware2>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        await mediator.Send<string>(new TestQuery { Value = 42 });

        tracker.Log.Should().Equal(
            "Middleware1-Before",
            "Middleware2-Before",
            "Middleware2-After",
            "Middleware1-After");
    }

    [Fact]
    public async Task Send_WithOpenGenericPipelineMiddleware_ExecutesCorrectly()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestQueryHandler>();
        services.AddMediatorPipelineMiddleware(typeof(GenericLoggingMiddleware<,>));
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        GenericLoggingMiddleware<TestQuery, string>.WasCalled = false;
        await mediator.Send<string>(new TestQuery { Value = 42 });

        GenericLoggingMiddleware<TestQuery, string>.WasCalled.Should().BeTrue();
    }
}
