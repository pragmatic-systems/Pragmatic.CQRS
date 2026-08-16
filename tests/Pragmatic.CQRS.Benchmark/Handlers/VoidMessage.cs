namespace Pragmatic.CQRS.Benchmark.Handlers;

public class VoidMessage : Pragmatic.CQRS.IRequest, MediatR.IRequest
{
    public VoidMessage(int count)
        => Count = count;

    public int Count { get; set; }
}

public class VoidMessageHandler
    : Pragmatic.CQRS.IRequestHandler<VoidMessage>,
      MediatR.IRequestHandler<VoidMessage>
{
    Task Pragmatic.CQRS.IRequestHandler<VoidMessage>.Handle(VoidMessage request, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    Task MediatR.IRequestHandler<VoidMessage>.Handle(VoidMessage request, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
