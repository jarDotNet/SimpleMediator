using Microsoft.Extensions.DependencyInjection;

namespace SimpleMediator.Tests;

/// <summary>
/// Base class for Mediator tests that provides common setup utilities.
/// </summary>
public abstract class MediatorTestBase
{
    /// <summary>
    /// Creates a fresh ServiceCollection and MediatorConfiguration for testing.
    /// The test assembly is automatically registered for handler discovery.
    /// </summary>
    protected (IServiceCollection Services, MediatorConfiguration Config) CreateManualSetup()
    {
        var services = new ServiceCollection();
        var config = new MediatorConfiguration();
        config.AddAssembly(GetType().Assembly);
        services.AddSingleton(config);
        services.AddSingleton<Mediator>();
        return (services, config);
    }
}

/// <summary>
/// Tracker for pipeline behavior execution order in tests.
/// </summary>
public class PipelineTracker
{
    public List<string> Log { get; } = [];

    public void Clear() => Log.Clear();
}
