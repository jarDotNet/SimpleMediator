using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Benchmarks.Requests;

namespace SimpleMediator.Benchmarks;

/// <summary>
/// Benchmarks to measure the impact of handler caching.
/// Each Mediator instance has its own cache, so the first call includes
/// handler wrapper creation, while subsequent calls benefit from the cached wrapper.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CachingBenchmarks
{
    private IServiceProvider _serviceProvider = null!;
    private Mediator _warmedUpMediator = null!;
    private SimpleCommand _command = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(CachingBenchmarks).Assembly);

        _serviceProvider = services.BuildServiceProvider();
        _warmedUpMediator = _serviceProvider.GetRequiredService<Mediator>();
        _command = new SimpleCommand("test");

        // Warm up the cached mediator
        _warmedUpMediator.Send<SimpleResponse>(_command).GetAwaiter().GetResult();
    }

    [Benchmark(Baseline = true, Description = "Cached handler (warm)")]
    public async Task<SimpleResponse> CachedHandler()
    {
        return await _warmedUpMediator.Send<SimpleResponse>(_command);
    }

    [Benchmark(Description = "New mediator instance (cold cache)")]
    public async Task<SimpleResponse> NewMediatorColdCache()
    {
        // Each new mediator instance has its own cache, so first call incurs compilation cost
        var mediator = new Mediator(_serviceProvider);
        return await mediator.Send<SimpleResponse>(_command);
    }
}
