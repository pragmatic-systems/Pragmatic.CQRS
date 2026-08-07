using System.Reflection.Metadata;
using MediatR;

namespace Pragmatic.CQRS.Benchmark.Handlers;

public class VoidPipelineMessage : Pragmatic.CQRS.IRequest, MediatR.IRequest
{
    public VoidPipelineMessage(int count)
        => Count = count;

    public int Count { get; set; }
}

public class VoidPipelineMessageHandler
    : Pragmatic.CQRS.IRequestHandler<VoidPipelineMessage>,
      MediatR.IRequestHandler<VoidPipelineMessage>
{
    Task Pragmatic.CQRS.IRequestHandler<VoidPipelineMessage>.Handle(VoidPipelineMessage request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    Task MediatR.IRequestHandler<VoidPipelineMessage>.Handle(VoidPipelineMessage request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class VoidPipelineBehaviourHandler
    : Pragmatic.CQRS.IPipelineBehavior<VoidPipelineMessage, Unit>,
      MediatR.IPipelineBehavior<VoidPipelineMessage, MediatR.Unit>
{
    async Task<Unit> Pragmatic.CQRS.IPipelineBehavior<VoidPipelineMessage, Unit>.Handle(VoidPipelineMessage input, Pragmatic.CQRS.RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken = default)
    {
        return await next();
    }

    async Task<MediatR.Unit> MediatR.IPipelineBehavior<VoidPipelineMessage, MediatR.Unit>.Handle(VoidPipelineMessage request, MediatR.RequestHandlerDelegate<MediatR.Unit> next, CancellationToken cancellationToken)
    {
        return await next();
    }
}
