using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Pragmatic.CQRS.Benchmark.Handlers;

namespace Pragmatic.CQRS.Benchmark.Benchmarks;

public class MediatorBenchmark
{
    private readonly IServiceProvider _provider;

    public MediatorBenchmark()
    {
        var services = new ServiceCollection();
        services.AddCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(typeof(MediatorBenchmark).Assembly);
        });

        services.AddTransient<IPipelineBehavior<VoidPipelineMessage>, VoidPipelineBehaviourHandler>();
        services.AddTransient<IPipelineBehavior<EchoPipelineMessage, int>, EchoPipelineBehaviourHandler>();

        _provider = services.BuildServiceProvider();
    }

    [Benchmark]
    public async Task RequestResponseRawBenchmark()
    {
        var mediator = _provider.GetRequiredService<IMediator>();
        await mediator.Send(new EchoMessage(1));
    }

    [Benchmark]
    public async Task RequestResponsePipelineBenchmark()
    {
        var mediator = _provider.GetRequiredService<IMediator>();
        await mediator.Send(new EchoPipelineMessage(1));
    }

    [Benchmark]
    public async Task RequestVoidRawBenchmark()
    {
        var mediator = _provider.GetRequiredService<IMediator>();
        await mediator.Send(new VoidMessage(1));
    }

    [Benchmark]
    public async Task RequestVoidPipelineBenchmark()
    {
        var mediator = _provider.GetRequiredService<IMediator>();
        await mediator.Send(new VoidPipelineMessage(1));
    }
}
