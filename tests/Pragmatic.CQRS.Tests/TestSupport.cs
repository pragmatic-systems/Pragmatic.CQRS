namespace Pragmatic.CQRS.Tests;

public record UnknownQuery : IRequest<string>;

public record LoggingQuery(int Value)
    : IRequest<int>;

public class LoggingQueryHandler : IRequestHandler<LoggingQuery, int>
{
    public int InvocationCount { get; private set; }

    public Task<int> Handle(LoggingQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        InvocationCount++;
        return Task.FromResult(query.Value * 2);
    }
}

public class LoggingBehavior : IPipelineBehavior<LoggingQuery, int>
{
    public string Name { get; }

    public List<string> Log { get; } = new();

    public LoggingBehavior(string name = "A", List<string>? logs = null)
    {
        Name = name;
        Log = logs ?? new List<string>();
    }

    public async Task<int> Handle(LoggingQuery input, RequestHandlerDelegate<int> next, CancellationToken cancellationToken = default)
    {
        Log.Add($"{Name}-before");
        var result = await next();
        Log.Add($"{Name}-after");
        return result;
    }
}

public record VoidLoggingCommand : IRequest;

public class VoidLoggingCommandHandler : IRequestHandler<VoidLoggingCommand>
{
    public int InvocationCount { get; private set; }

    public Task Handle(VoidLoggingCommand query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        InvocationCount++;
        return Task.CompletedTask;
    }
}

public class VoidLoggingBehavior : IPipelineBehavior<VoidLoggingCommand>
{
    public VoidLoggingBehavior(string name, List<string> logs)
    {
        Name = name;
        Log = logs;
    }

    public string Name { get; }

    public List<string> Log { get; }

    public async Task Handle(VoidLoggingCommand input, RequestHandlerDelegate next, CancellationToken cancellationToken = default)
    {
        Log.Add($"{Name}-before");
        await next();
        Log.Add($"{Name}-after");
    }
}

// --- Notification support types ---
public record DomainEventOccurred : INotification
{
    public string EventName { get; }

    public DomainEventOccurred(string eventName) => EventName = eventName;
}

public class DomainEventFirstHandler : INotificationHandler<DomainEventOccurred>
{
    public int InvocationCount { get; private set; }

    public string? ReceivedEventName { get; private set; }

    public Task Handle(DomainEventOccurred notification, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        InvocationCount++;
        ReceivedEventName = notification.EventName;
        return Task.CompletedTask;
    }
}

public class DomainEventSecondHandler : INotificationHandler<DomainEventOccurred>
{
    public int InvocationCount { get; private set; }

    public string? ReceivedEventName { get; private set; }

    public Task Handle(DomainEventOccurred notification, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        ReceivedEventName = notification.EventName;
        return Task.CompletedTask;
    }
}

public class AsyncFirstErrorHandler : INotificationHandler<DomainEventOccurred>
{
    public async Task Handle(DomainEventOccurred notification, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100);
        throw new ApplicationException();
    }
}

public class AsyncSecondErrorHandler : INotificationHandler<DomainEventOccurred>
{
    public async Task Handle(DomainEventOccurred notification, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100);
        throw new ApplicationException();
    }
}

public class SyncFirstErrorHandler : INotificationHandler<DomainEventOccurred>
{
    public Task Handle(DomainEventOccurred notification, CancellationToken cancellationToken = default)
    {
        throw new ApplicationException();
    }
}

public class SyncSecondErrorHandler : INotificationHandler<DomainEventOccurred>
{
    public Task Handle(DomainEventOccurred notification, CancellationToken cancellationToken = default)
    {
        throw new ApplicationException();
    }
}

public record CancellationNotification : INotification { }

public class CancellationNotificationHandler : INotificationHandler<CancellationNotification>
{
    public Task Handle(CancellationNotification notification, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
