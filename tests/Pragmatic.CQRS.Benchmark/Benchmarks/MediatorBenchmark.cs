using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Pragmatic.CQRS.Benchmark.Handlers;

namespace Pragmatic.CQRS.Benchmark.Benchmarks;

public class MediatorBenchmark
{
    private readonly IServiceProvider _pragmaProvider;
    private readonly IServiceProvider _mediatrProvider;

    public MediatorBenchmark()
    {
        // Pragmatic.CQRS DI setup
        var pragmaServices = new ServiceCollection();
        pragmaServices.AddCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(typeof(MediatorBenchmark).Assembly);
        });

        pragmaServices.AddTransient<Pragmatic.CQRS.IPipelineBehavior<VoidPipelineMessage>, VoidPipelineBehaviourHandler>();
        pragmaServices.AddTransient<Pragmatic.CQRS.IPipelineBehavior<EchoPipelineMessage, int>, EchoPipelineBehaviourHandler>();

        _pragmaProvider = pragmaServices.BuildServiceProvider();

        // MediatR DI setup
        var mediatrServices = new ServiceCollection();
        mediatrServices.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(MediatorBenchmark).Assembly);
        });

        _mediatrProvider = mediatrServices.BuildServiceProvider();
    }

    // --- Pragmatic.CQRS benchmarks ---
    [Benchmark(Baseline = true)]
    public async Task Pragma_RequestResponseRaw()
    {
        var mediator = _pragmaProvider.GetRequiredService<Pragmatic.CQRS.IMediator>();
        await mediator.Send(new EchoMessage(1));
    }

    [Benchmark]
    public async Task Pragma_RequestResponsePipeline()
    {
        var mediator = _pragmaProvider.GetRequiredService<Pragmatic.CQRS.IMediator>();
        await mediator.Send(new EchoPipelineMessage(1));
    }

    [Benchmark]
    public async Task Pragma_RequestVoidRaw()
    {
        var mediator = _pragmaProvider.GetRequiredService<Pragmatic.CQRS.IMediator>();
        await mediator.Send(new VoidMessage(1));
    }

    [Benchmark]
    public async Task Pragma_RequestVoidPipeline()
    {
        var mediator = _pragmaProvider.GetRequiredService<Pragmatic.CQRS.IMediator>();
        await mediator.Send(new VoidPipelineMessage(1));
    }

    // --- MediatR benchmarks ---
    [Benchmark]
    public async Task MediatR_RequestResponseRaw()
    {
        var mediator = _mediatrProvider.GetRequiredService<ISender>();
        await mediator.Send(new EchoMessage(1));
    }

    [Benchmark]
    public async Task MediatR_RequestResponsePipeline()
    {
        var mediator = _mediatrProvider.GetRequiredService<ISender>();
        await mediator.Send(new EchoPipelineMessage(1));
    }

    [Benchmark]
    public async Task MediatR_RequestVoidRaw()
    {
        var mediator = _mediatrProvider.GetRequiredService<ISender>();
        await mediator.Send(new VoidMessage(1));
    }

    [Benchmark]
    public async Task MediatR_RequestVoidPipeline()
    {
        var mediator = _mediatrProvider.GetRequiredService<ISender>();
        await mediator.Send(new VoidPipelineMessage(1));
    }
}
