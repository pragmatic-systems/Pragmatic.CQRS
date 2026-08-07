using System.Reflection.Metadata;

namespace Pragmatic.CQRS.Benchmark.Handlers;

public class EchoMessage : Pragmatic.CQRS.IRequest<int>, MediatR.IRequest<int>
{
    public EchoMessage(int count)
        => Count = count;

    public int Count { get; set; }
}

public class EchoMessageHandler
    : Pragmatic.CQRS.IRequestHandler<EchoMessage, int>,
      MediatR.IRequestHandler<EchoMessage, int>
{
    Task<int> Pragmatic.CQRS.IRequestHandler<EchoMessage, int>.Handle(EchoMessage request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Count);
    }

    Task<int> MediatR.IRequestHandler<EchoMessage, int>.Handle(EchoMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Count);
    }
}

public class EchoPipelineMessage : Pragmatic.CQRS.IRequest<int>, MediatR.IRequest<int>
{
    public EchoPipelineMessage(int count)
        => Count = count;

    public int Count { get; set; }
}

public class EchoPipelineMessageHandler
    : Pragmatic.CQRS.IRequestHandler<EchoPipelineMessage, int>,
      MediatR.IRequestHandler<EchoPipelineMessage, int>
{
    Task<int> Pragmatic.CQRS.IRequestHandler<EchoPipelineMessage, int>.Handle(EchoPipelineMessage request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Count);
    }

    Task<int> MediatR.IRequestHandler<EchoPipelineMessage, int>.Handle(EchoPipelineMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Count);
    }
}

public class EchoPipelineBehaviourHandler
    : Pragmatic.CQRS.IPipelineBehavior<EchoPipelineMessage, int>,
      MediatR.IPipelineBehavior<EchoPipelineMessage, int>
{
    async Task<int> Pragmatic.CQRS.IPipelineBehavior<EchoPipelineMessage, int>.Handle(EchoPipelineMessage input, Pragmatic.CQRS.RequestHandlerDelegate<int> next, CancellationToken cancellationToken = default)
    {
        return await next();
    }

    async Task<int> MediatR.IPipelineBehavior<EchoPipelineMessage, int>.Handle(EchoPipelineMessage request, MediatR.RequestHandlerDelegate<int> next, CancellationToken cancellationToken)
    {
        return await next();
    }
}
