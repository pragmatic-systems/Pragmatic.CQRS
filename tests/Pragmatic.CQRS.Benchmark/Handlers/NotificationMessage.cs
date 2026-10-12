namespace Pragmatic.CQRS.Benchmark.Handlers;

public class EchoNotification : Pragmatic.CQRS.INotification, MediatR.INotification
{
    public EchoNotification(int count)
        => Count = count;

    public int Count { get; set; }
}

public class EchoNotificationFirstHandler
    : Pragmatic.CQRS.INotificationHandler<EchoNotification>,
      MediatR.INotificationHandler<EchoNotification>
{
    Task Pragmatic.CQRS.INotificationHandler<EchoNotification>.Handle(EchoNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    Task MediatR.INotificationHandler<EchoNotification>.Handle(EchoNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class EchoNotificationSecondHandler
    : Pragmatic.CQRS.INotificationHandler<EchoNotification>,
      MediatR.INotificationHandler<EchoNotification>
{
    Task Pragmatic.CQRS.INotificationHandler<EchoNotification>.Handle(EchoNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    Task MediatR.INotificationHandler<EchoNotification>.Handle(EchoNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
