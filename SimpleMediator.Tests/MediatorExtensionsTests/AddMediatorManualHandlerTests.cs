using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleMediator.Tests.MediatorExtensionsTests;

/// <summary>
/// Tests for the AddMediatorManualHandler extension method.
/// </summary>
public class AddMediatorManualHandlerTests : MediatorTestBase
{
    [Fact]
    public void AddMediatorManualHandler_RegistersSpecificHandler()
    {
        var services = new ServiceCollection();
        services.AddMediator();
        services.AddMediatorManualHandler<ManualTestHandler1>();
        var serviceProvider = services.BuildServiceProvider();

        serviceProvider.GetService<ManualTestHandler1>().Should().NotBeNull();
    }

    [Fact]
    public void AddMediatorManualHandler_AddsHandlerAssemblyToConfiguration()
    {
        var services = new ServiceCollection();
        services.AddMediator();
        services.AddMediatorManualHandler<ManualTestHandler1>();
        var serviceProvider = services.BuildServiceProvider();

        var config = serviceProvider.GetRequiredService<MediatorConfiguration>();

        config.HandlerAssemblies.Should().Contain(typeof(ManualTestHandler1).Assembly);
    }
}
