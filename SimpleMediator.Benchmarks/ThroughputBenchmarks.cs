using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Benchmarks.Requests;

namespace SimpleMediator.Benchmarks;

/// <summary>
/// Benchmarks for measuring throughput with varying numbers of requests.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ThroughputBenchmarks
{
    private Mediator _mediator = null!;
    private SimpleCommand[] _commands = null!;
    private SimpleEvent[] _events = null!;

    [Params(10, 100, 1000)]
    public int RequestCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(ThroughputBenchmarks).Assembly);
        services.AddMediatorManualHandler<SimpleEventHandler1>();
        services.AddMediatorManualHandler<SimpleEventHandler2>();
        services.AddMediatorManualHandler<SimpleEventHandler3>();

        _mediator = services.BuildServiceProvider().GetRequiredService<Mediator>();

        // Pre-create requests
        _commands = Enumerable.Range(0, RequestCount)
            .Select(i => new SimpleCommand($"command-{i}"))
            .ToArray();

        _events = Enumerable.Range(0, RequestCount)
            .Select(i => new SimpleEvent(i, $"event-{i}"))
            .ToArray();

        // Warm up cache
        _mediator.Send<SimpleResponse>(_commands[0]).GetAwaiter().GetResult();
        _mediator.Publish(_events[0]).GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Sequential commands")]
    public async Task SequentialCommands()
    {
        foreach (var command in _commands)
        {
            await _mediator.Send<SimpleResponse>(command);
        }
    }

    [Benchmark(Description = "Parallel commands")]
    public async Task ParallelCommands()
    {
        await Task.WhenAll(_commands.Select(c => _mediator.Send<SimpleResponse>(c)));
    }

    [Benchmark(Description = "Sequential events")]
    public async Task SequentialEvents()
    {
        foreach (var @event in _events)
        {
            await _mediator.Publish(@event);
        }
    }

    [Benchmark(Description = "Parallel events")]
    public async Task ParallelEvents()
    {
        await Task.WhenAll(_events.Select(e => _mediator.Publish(e)));
    }
}
