using FluentAssertions;
using System.Reflection;

namespace SimpleMediator.Tests;

public class MediatorConfigurationTests : MediatorTestBase
{
    [Fact]
    public void Constructor_InitializesEmptyHandlerAssemblies()
    {
        var config = new MediatorConfiguration();

        config.HandlerAssemblies.Should().NotBeNull();
        config.HandlerAssemblies.Should().BeEmpty();
    }

    [Fact]
    public void AddAssembly_AddsAssemblyToCollection()
    {
        var config = new MediatorConfiguration();
        var assembly = Assembly.GetExecutingAssembly();

        config.AddAssembly(assembly);

        config.HandlerAssemblies.Should().Contain(assembly);
        config.HandlerAssemblies.Should().HaveCount(1);
    }

    [Fact]
    public void AddAssembly_WithMultipleAssemblies_AddsAllToCollection()
    {
        var config = new MediatorConfiguration();
        var assembly1 = Assembly.GetExecutingAssembly();
        var assembly2 = typeof(Mediator).Assembly;

        config.AddAssembly(assembly1);
        config.AddAssembly(assembly2);

        config.HandlerAssemblies.Should().Contain(assembly1);
        config.HandlerAssemblies.Should().Contain(assembly2);
        config.HandlerAssemblies.Should().HaveCount(2);
    }

    [Fact]
    public void AddAssembly_WithDuplicateAssembly_DoesNotAddDuplicate()
    {
        var config = new MediatorConfiguration();
        var assembly = Assembly.GetExecutingAssembly();

        config.AddAssembly(assembly);
        config.AddAssembly(assembly);
        config.AddAssembly(assembly);

        config.HandlerAssemblies.Should().HaveCount(1);
    }

    [Fact]
    public void HandlerAssemblies_IsHashSet_EnsuresUniqueness()
    {
        var config = new MediatorConfiguration();

        config.HandlerAssemblies.Should().BeAssignableTo<HashSet<Assembly>>();
    }
}
