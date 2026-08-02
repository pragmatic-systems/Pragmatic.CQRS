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
    ValueTask Pragmatic.CQRS.IRequestHandler<VoidPipelineMessage>.Handle(VoidPipelineMessage request, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }

    Task MediatR.IRequestHandler<VoidPipelineMessage>.Handle(VoidPipelineMessage request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class VoidPipelineBehaviourHandler
    : Pragmatic.CQRS.IPipelineBehavior<VoidPipelineMessage>,
      MediatR.IPipelineBehavior<VoidPipelineMessage, Unit>
{
    async ValueTask Pragmatic.CQRS.IPipelineBehavior<VoidPipelineMessage>.Handle(VoidPipelineMessage input, Pragmatic.CQRS.RequestHandlerDelegate next, CancellationToken cancellationToken = default)
    {
        await next();
    }

    async Task<Unit> MediatR.IPipelineBehavior<VoidPipelineMessage, Unit>.Handle(VoidPipelineMessage request, MediatR.RequestHandlerDelegate<Unit> next, CancellationToken cancellationToken)
    {
        return await next();
    }
}
