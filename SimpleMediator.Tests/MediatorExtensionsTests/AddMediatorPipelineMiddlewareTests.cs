using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Tests.MediatorTests;
using System.Reflection;

namespace SimpleMediator.Tests.MediatorExtensionsTests;

/// <summary>
/// Tests for the AddMediatorPipelineMiddleware extension methods.
/// </summary>
public class AddMediatorPipelineMiddlewareTests : MediatorTestBase
{
    [Fact]
    public void AddMediatorPipelineMiddleware_RegistersSpecificMiddlewareAsTransient()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        services.AddSingleton<PipelineTracker>();
        services.AddMediatorPipelineMiddleware<TestQuery, string, TestPipelineMiddleware>();
        var serviceProvider = services.BuildServiceProvider();

        var middleware1 = serviceProvider.GetService<IPipelineMiddleware<TestQuery, string>>();
        var middleware2 = serviceProvider.GetService<IPipelineMiddleware<TestQuery, string>>();

        middleware1.Should().NotBeNull();
        middleware2.Should().NotBeNull();
        middleware1.Should().NotBeSameAs(middleware2);
    }

    [Fact]
    public void AddMediatorPipelineMiddleware_AllowsMultipleMiddlewaresForSameRequestType()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        services.AddSingleton<PipelineTracker>();
        services.AddMediatorPipelineMiddleware<TestQuery, string, TestPipelineMiddleware1>();
        services.AddMediatorPipelineMiddleware<TestQuery, string, TestPipelineMiddleware2>();
        var serviceProvider = services.BuildServiceProvider();

        var middlewares = serviceProvider.GetServices<IPipelineMiddleware<TestQuery, string>>().ToList();

        middlewares.Should().HaveCount(2);
        middlewares.Should().ContainItemsAssignableTo<IPipelineMiddleware<TestQuery, string>>();
    }

    [Fact]
    public void AddMediatorPipelineMiddleware_WithOpenGenericType_RegistersCorrectly()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        services.AddMediatorPipelineMiddleware(typeof(GenericLoggingMiddleware<,>));
        var serviceProvider = services.BuildServiceProvider();

        var middleware = serviceProvider.GetService<IPipelineMiddleware<TestQuery, string>>();

        middleware.Should().NotBeNull();
        middleware.Should().BeOfType<GenericLoggingMiddleware<TestQuery, string>>();
    }

    [Fact]
    public void AddMediatorPipelineMiddleware_WithClosedGenericType_ThrowsArgumentException()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        services.AddSingleton<PipelineTracker>();

        var act = () => services.AddMediatorPipelineMiddleware(typeof(TestPipelineMiddleware));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be an open generic type*");
    }

    [Fact]
    public void AddMediatorPipelineMiddleware_WithMultipleOpenGenerics_RegistersAll()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        services.AddMediatorPipelineMiddleware(typeof(GenericLoggingMiddleware<,>));
        services.AddMediatorPipelineMiddleware(typeof(GenericValidationMiddleware<,>));
        var serviceProvider = services.BuildServiceProvider();

        var middlewares = serviceProvider.GetServices<IPipelineMiddleware<TestQuery, string>>().ToList();

        middlewares.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddMediatorPipelineMiddleware_ExecutesMiddlewareDuringRequest()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        var tracker = new PipelineTracker();
        services.AddSingleton(tracker);
        services.AddMediatorPipelineMiddleware<TestQuery, string, TestPipelineMiddleware>();
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<Mediator>();

        var result = await mediator.Send<string>(new TestQuery { Value = 42 });

        result.Should().Be("Result: 42");
        tracker.Log.Should().Equal("Before", "After");
    }
}
