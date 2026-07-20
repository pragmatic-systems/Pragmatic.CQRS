using Pragmatic.CQRS;

namespace Pragmatic.CQRS.Benchmark.Handlers;

public class VoidPipelineMessage : IRequest
{
    public VoidPipelineMessage(int count)
        => Count = count;

    public int Count { get; set; }
}

public class VoidPipelineMessageHandler : IRequestHandler<VoidPipelineMessage>
{
    public ValueTask Handle(VoidPipelineMessage request, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}

public class VoidPipelineBehaviourHandler : IPipelineBehavior<VoidPipelineMessage>
{
    public async ValueTask Handle(VoidPipelineMessage input, RequestHandlerDelegate next, CancellationToken cancellationToken = default)
    {
        await next();
    }
}
