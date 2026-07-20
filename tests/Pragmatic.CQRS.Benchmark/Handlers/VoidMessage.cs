using Pragmatic.CQRS;

namespace Pragmatic.CQRS.Benchmark.Handlers;

public class VoidMessage : IRequest
{
    public VoidMessage(int count)
        => Count = count;

    public int Count { get; set; }
}

public class VoidMessageHandler : IRequestHandler<VoidMessage>
{
    public ValueTask Handle(VoidMessage request, CancellationToken cancellationToken = default)
    {
        return ValueTask.CompletedTask;
    }
}
