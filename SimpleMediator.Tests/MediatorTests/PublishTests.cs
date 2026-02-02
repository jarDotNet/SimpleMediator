using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMediator.Tests.MediatorTests;

public class PublishTests : MediatorTestBase
{
    [Fact]
    public async Task Publish_WithMultipleHandlers_ExecutesAllHandlers()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestEventHandler1>();
        services.AddTransient<TestEventHandler2>();
        services.AddTransient<TestEventHandler3>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        TestEventHandler1.ExecutionCount = 0;
        TestEventHandler2.ExecutionCount = 0;
        TestEventHandler3.ExecutionCount = 0;

        await mediator.Publish(new TestEvent { Message = "Test" });

        TestEventHandler1.ExecutionCount.Should().Be(1);
        TestEventHandler2.ExecutionCount.Should().Be(1);
        TestEventHandler3.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task Publish_WithGenericOverload_ExecutesAllHandlers()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestEventHandler1>();
        services.AddTransient<TestEventHandler2>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        TestEventHandler1.ExecutionCount = 0;
        TestEventHandler2.ExecutionCount = 0;

        await mediator.Publish<TestEvent>(new TestEvent { Message = "Generic" });

        TestEventHandler1.ExecutionCount.Should().Be(1);
        TestEventHandler2.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task Publish_WithNoHandlers_CompletesSuccessfully()
    {
        var (services, _) = CreateManualSetup();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        var act = async () => await mediator.Publish(new EventWithoutHandlers());

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Publish_WithCancellationToken_PassesTokenToHandlers()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestEventHandler2>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        TestEventHandler2.ExecutionCount = 0;
        using var cts = new CancellationTokenSource();

        await mediator.Publish(new TestEvent(), cts.Token);

        TestEventHandler2.ExecutionCount.Should().Be(1);
    }

    [Fact]
    public async Task Publish_WithNullEvent_ThrowsArgumentNullException()
    {
        var (services, _) = CreateManualSetup();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        var act = async () => await mediator.Publish(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Publish_CachesEventPublisher_ForBetterPerformance()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestEventHandler1>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        TestEventHandler1.ExecutionCount = 0;
        await mediator.Publish(new TestEvent());
        await mediator.Publish(new TestEvent());

        TestEventHandler1.ExecutionCount.Should().Be(2);
    }
}
