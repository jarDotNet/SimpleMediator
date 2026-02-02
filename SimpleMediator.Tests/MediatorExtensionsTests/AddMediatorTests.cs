using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Tests.MediatorTests;
using System.Reflection;

namespace SimpleMediator.Tests.MediatorExtensionsTests;

/// <summary>
/// Tests for the AddMediator extension method.
/// </summary>
public class AddMediatorTests : MediatorTestBase
{
    [Fact]
    public void AddMediator_RegistersMediatorAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();

        var mediator1 = serviceProvider.GetService<Mediator>();
        var mediator2 = serviceProvider.GetService<Mediator>();

        mediator1.Should().NotBeNull();
        mediator2.Should().NotBeNull();
        mediator1.Should().BeSameAs(mediator2);
    }

    [Fact]
    public void AddMediator_RegistersConfigurationAsSingleton()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();

        var config1 = serviceProvider.GetService<MediatorConfiguration>();
        var config2 = serviceProvider.GetService<MediatorConfiguration>();

        config1.Should().NotBeNull();
        config2.Should().NotBeNull();
        config1.Should().BeSameAs(config2);
    }

    [Fact]
    public void AddMediator_AutoRegistersHandlersFromSpecifiedAssembly()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();

        // These should be registered because they end with "Handler"
        serviceProvider.GetService<TestQueryHandler>().Should().NotBeNull();
        serviceProvider.GetService<TestCommandHandler>().Should().NotBeNull();
    }

    [Fact]
    public void AddMediator_WithMultipleAssemblies_RegistersHandlersFromAllAssemblies()
    {
        var services = new ServiceCollection();
        var assembly1 = Assembly.GetExecutingAssembly();
        var assembly2 = typeof(Mediator).Assembly;

        services.AddMediator(assembly1, assembly2);
        var serviceProvider = services.BuildServiceProvider();

        var config = serviceProvider.GetRequiredService<MediatorConfiguration>();

        config.HandlerAssemblies.Should().Contain(assembly1);
        config.HandlerAssemblies.Should().Contain(assembly2);
        serviceProvider.GetService<Mediator>().Should().NotBeNull();
    }

    [Fact]
    public void AddMediator_WithNoAssemblies_UsesCallingAssembly()
    {
        var services = new ServiceCollection();
        services.AddMediator();
        var serviceProvider = services.BuildServiceProvider();

        var config = serviceProvider.GetRequiredService<MediatorConfiguration>();

        config.HandlerAssemblies.Should().HaveCountGreaterThan(0);
        serviceProvider.GetService<Mediator>().Should().NotBeNull();
    }

    [Fact]
    public void AddMediator_RegistersHandlersAsTransient()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();

        var handler1 = serviceProvider.GetService<TestQueryHandler>();
        var handler2 = serviceProvider.GetService<TestQueryHandler>();

        handler1.Should().NotBeNull();
        handler2.Should().NotBeNull();
        handler1.Should().NotBeSameAs(handler2);
    }

    [Fact]
    public void AddMediator_DoesNotRegisterAbstractClasses()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<AbstractTestHandler>().Should().BeNull();
    }

    [Fact]
    public void AddMediator_DoesNotRegisterInterfaces()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<ITestHandler>().Should().BeNull();
    }

    [Fact]
    public void AddMediator_OnlyRegistersTypesEndingWithHandler()
    {
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());
        var serviceProvider = services.BuildServiceProvider();

        // This class name doesn't end with "Handler"
        serviceProvider.GetService<NotAutoRegistered>().Should().BeNull();
    }

    [Fact]
    public void AddMediator_PreventsAssemblyDuplication()
    {
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediator(assembly, assembly, assembly);
        var serviceProvider = services.BuildServiceProvider();

        var config = serviceProvider.GetRequiredService<MediatorConfiguration>();

        // HashSet should prevent duplicates
        config.HandlerAssemblies.Count(a => a == assembly).Should().Be(1);
    }
}
