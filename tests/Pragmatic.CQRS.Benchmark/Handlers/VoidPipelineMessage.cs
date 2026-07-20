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
    public Task Handle(VoidPipelineMessage request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(request.Count);
    }
}

public class VoidPipelineBehaviourHandler : IPipelineBehavior<VoidPipelineMessage>
{
    public Task Handle(VoidPipelineMessage input, RequestHandlerDelegate next, CancellationToken cancellationToken = default)
    {
        return next();
    }
}
