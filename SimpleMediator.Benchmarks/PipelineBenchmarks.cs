using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.DependencyInjection;
using SimpleMediator.Benchmarks.Middlewares;
using SimpleMediator.Benchmarks.Requests;

namespace SimpleMediator.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PipelineBenchmarks
{
    private Mediator _noPipelineMediator = null!;
    private Mediator _oneMiddlewareMediator = null!;
    private Mediator _twoMiddlewaresMediator = null!;
    private Mediator _threeMiddlewaresMediator = null!;
    private SimpleCommand _command = null!;

    [GlobalSetup]
    public void Setup()
    {
        _command = new SimpleCommand("benchmark");

        // Setup mediator without pipeline
        _noPipelineMediator = CreateMediator(0);

        // Setup mediator with 1 pipeline middleware
        _oneMiddlewareMediator = CreateMediator(1);

        // Setup mediator with 2 pipeline middlewares
        _twoMiddlewaresMediator = CreateMediator(2);

        // Setup mediator with 3 pipeline middlewares
        _threeMiddlewaresMediator = CreateMediator(3);

        // Warm up all mediators
        _noPipelineMediator.Send<SimpleResponse>(_command).GetAwaiter().GetResult();
        _oneMiddlewareMediator.Send<SimpleResponse>(_command).GetAwaiter().GetResult();
        _twoMiddlewaresMediator.Send<SimpleResponse>(_command).GetAwaiter().GetResult();
        _threeMiddlewaresMediator.Send<SimpleResponse>(_command).GetAwaiter().GetResult();
    }

    private static Mediator CreateMediator(int middlewareCount)
    {
        var services = new ServiceCollection();
        services.AddMediator(typeof(PipelineBenchmarks).Assembly);

        if (middlewareCount >= 1)
            services.AddMediatorPipelineMiddleware(typeof(PassThroughMiddleware<,>));

        if (middlewareCount >= 2)
            services.AddMediatorPipelineMiddleware(typeof(SecondPassThroughMiddleware<,>));

        if (middlewareCount >= 3)
            services.AddMediatorPipelineMiddleware(typeof(ThirdPassThroughMiddleware<,>));

        return services.BuildServiceProvider().GetRequiredService<Mediator>();
    }

    [Benchmark(Baseline = true, Description = "No pipeline")]
    public async Task<SimpleResponse> NoPipeline()
    {
        return await _noPipelineMediator.Send<SimpleResponse>(_command);
    }

    [Benchmark(Description = "1 pipeline middleware")]
    public async Task<SimpleResponse> OneMiddleware()
    {
        return await _oneMiddlewareMediator.Send<SimpleResponse>(_command);
    }

    [Benchmark(Description = "2 pipeline middlewares")]
    public async Task<SimpleResponse> TwoMiddlewares()
    {
        return await _twoMiddlewaresMediator.Send<SimpleResponse>(_command);
    }

    [Benchmark(Description = "3 pipeline middlewares")]
    public async Task<SimpleResponse> ThreeMiddlewares()
    {
        return await _threeMiddlewaresMediator.Send<SimpleResponse>(_command);
    }
}
