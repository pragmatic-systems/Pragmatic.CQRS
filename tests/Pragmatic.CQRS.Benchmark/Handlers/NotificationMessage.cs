namespace Pragmatic.CQRS.Benchmark.Handlers;

public class EchoNotification : Pragmatic.CQRS.INotification, MediatR.INotification
{
    public EchoNotification(int count)
        => Count = count;

    public int Count { get; set; }
}

// NOTE: Due to the significant performance / memory allocation difference between Pragmatic implementation and MediatR, I added a 1ms delay and confirmed the numbers are inflating correctly on both sides.

public class EchoNotificationFirstHandler
    : Pragmatic.CQRS.INotificationHandler<EchoNotification>,
      MediatR.INotificationHandler<EchoNotification>
{
    Task Pragmatic.CQRS.INotificationHandler<EchoNotification>.Handle(EchoNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.Delay(1);
    }

    Task MediatR.INotificationHandler<EchoNotification>.Handle(EchoNotification notification, CancellationToken cancellationToken)
    {
        return Task.Delay(1);
    }
}

public class EchoNotificationSecondHandler
    : Pragmatic.CQRS.INotificationHandler<EchoNotification>,
      MediatR.INotificationHandler<EchoNotification>
{
    Task Pragmatic.CQRS.INotificationHandler<EchoNotification>.Handle(EchoNotification notification, CancellationToken cancellationToken = default)
    {
        return Task.Delay(1);
    }

    Task MediatR.INotificationHandler<EchoNotification>.Handle(EchoNotification notification, CancellationToken cancellationToken)
    {
        return Task.Delay(1);
    }
}
