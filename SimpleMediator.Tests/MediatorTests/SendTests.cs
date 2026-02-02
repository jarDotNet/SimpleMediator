using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMediator.Tests.MediatorTests;

public class SendTests : MediatorTestBase
{
    [Fact]
    public async Task Send_WithValidQueryHandler_ReturnsExpectedResult()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestQueryHandler>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        var result = await mediator.Send<string>(new TestQuery { Value = 42 });

        result.Should().Be("Result: 42");
    }

    [Fact]
    public async Task Send_WithValidCommand_ExecutesSuccessfully()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestCommandHandler>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        var command = new TestCommand { Value = 100 };

        await mediator.Send(command);

        command.Value.Should().Be(100);
    }

    [Fact]
    public async Task Send_WithCancellationToken_PassesTokenToHandler()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestCancellableQueryHandler>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        using var cts = new CancellationTokenSource();
        var result = await mediator.Send<bool>(new TestCancellableQuery(), cts.Token);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Send_WhenHandlerNotRegisteredInAssembly_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var config = new MediatorConfiguration(); // Empty config - no assemblies
        services.AddSingleton(config);
        services.AddSingleton<Mediator>();

        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();
        var act = async () => await mediator.Send<string>(new TestQuery { Value = 42 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*No handler found*");
    }

    [Fact]
    public async Task Send_WithWrongReturnType_ThrowsInvalidOperationException()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestQueryHandler>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        var act = async () => await mediator.Send<int>(new TestQuery { Value = 42 });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*returns String*but Int32 was expected*");
    }

    [Fact]
    public async Task Send_WithNullRequest_ThrowsArgumentNullException()
    {
        var (services, _) = CreateManualSetup();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        var act = async () => await mediator.Send<string>(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Send_WithSynchronousHandler_ExecutesSuccessfully()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<SyncTestQueryHandler>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        var result = await mediator.Send<int>(new SyncTestQuery { Value = 99 });

        result.Should().Be(99);
    }

    [Fact]
    public async Task Send_WithHandlerThatReturnsTask_WorksCorrectly()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestCommandHandler>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        await mediator.Send(new TestCommand { Value = 50 });
    }

    [Fact]
    public async Task Send_WithHandlerThatReturnsTaskOfT_WorksCorrectly()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestQueryHandler>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        var result = await mediator.Send<string>(new TestQuery { Value = 100 });

        result.Should().Be("Result: 100");
    }

    [Fact]
    public async Task Send_CachesHandlerWrapper_ForBetterPerformance()
    {
        var (services, _) = CreateManualSetup();
        services.AddTransient<TestQueryHandler>();
        var mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        await mediator.Send<string>(new TestQuery { Value = 1 });
        var result = await mediator.Send<string>(new TestQuery { Value = 2 });

        result.Should().Be("Result: 2");
    }
}
