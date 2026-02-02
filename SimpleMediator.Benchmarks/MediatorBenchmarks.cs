using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Benchmarks.Requests;

namespace SimpleMediator.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class MediatorBenchmarks
{
    private Mediator _mediator = null!;
    private SimpleCommand _command = null!;
    private VoidCommand _voidCommand = null!;
    private SimpleQuery _query = null!;
    private SimpleEvent _event = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();

        services.AddMediator(typeof(MediatorBenchmarks).Assembly);

        // Register event handlers manually
        services.AddMediatorManualHandler<SimpleEventHandler1>();
        services.AddMediatorManualHandler<SimpleEventHandler2>();
        services.AddMediatorManualHandler<SimpleEventHandler3>();

        var provider = services.BuildServiceProvider();
        _mediator = provider.GetRequiredService<Mediator>();

        // Pre-create requests
        _command = new SimpleCommand("test");
        _voidCommand = new VoidCommand("test");
        _query = new SimpleQuery(42);
        _event = new SimpleEvent(1, "test message");

        // Warm up caches
        _mediator.Send<SimpleResponse>(_command).GetAwaiter().GetResult();
        _mediator.Send(_voidCommand).GetAwaiter().GetResult();
        _mediator.Send<SimpleResponse>(_query).GetAwaiter().GetResult();
        _mediator.Publish(_event).GetAwaiter().GetResult();
    }

    [Benchmark(Description = "Send<TResponse>(command)")]
    public async Task<SimpleResponse> SendCommand()
    {
        return await _mediator.Send<SimpleResponse>(_command);
    }

    [Benchmark(Description = "Send(command) - void")]
    public async Task SendVoidCommand()
    {
        await _mediator.Send(_voidCommand);
    }

    [Benchmark(Description = "Send<TResponse>(query)")]
    public async Task<SimpleResponse> SendQuery()
    {
        return await _mediator.Send<SimpleResponse>(_query);
    }

    [Benchmark(Description = "Publish(event) - 3 handlers")]
    public async Task PublishEvent()
    {
        await _mediator.Publish(_event);
    }
}
