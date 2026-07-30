namespace Pragmatic.CQRS.Tests;

public record UnknownQuery : IRequest<string>;

public record LoggingQuery(int Value)
    : IRequest<int>;

public class LoggingQueryHandler : IRequestHandler<LoggingQuery, int>
{
    public int InvocationCount { get; private set; }

    public ValueTask<int> Handle(LoggingQuery query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        InvocationCount++;
        return new ValueTask<int>(Task.FromResult(query.Value * 2));
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

    public async ValueTask<int> Handle(LoggingQuery input, RequestHandlerDelegate<int> next, CancellationToken cancellationToken = default)
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

    public ValueTask Handle(VoidLoggingCommand query, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        InvocationCount++;
        return new ValueTask(Task.CompletedTask);
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

    public async ValueTask Handle(VoidLoggingCommand input, RequestHandlerDelegate next, CancellationToken cancellationToken = default)
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

    public ValueTask Handle(DomainEventOccurred notification, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        InvocationCount++;
        ReceivedEventName = notification.EventName;
        return ValueTask.CompletedTask;
    }
}

public class DomainEventSecondHandler : INotificationHandler<DomainEventOccurred>
{
    public int InvocationCount { get; private set; }

    public string? ReceivedEventName { get; private set; }

    public ValueTask Handle(DomainEventOccurred notification, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        ReceivedEventName = notification.EventName;
        return ValueTask.CompletedTask;
    }
}

public class AsyncErrorHandler : INotificationHandler<DomainEventOccurred>
{
    public async ValueTask Handle(DomainEventOccurred notification, CancellationToken cancellationToken = default)
    {
        await Task.Delay(100);
        throw new ApplicationException();
    }
}

public class SyncErrorHandler : INotificationHandler<DomainEventOccurred>
{
    public ValueTask Handle(DomainEventOccurred notification, CancellationToken cancellationToken = default)
    {
        throw new ApplicationException();
    }
}

public record CancellationNotification : INotification { }

public class CancellationNotificationHandler : INotificationHandler<CancellationNotification>
{
    public ValueTask Handle(CancellationNotification notification, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.CompletedTask;
    }
}

// --- Open Generic Pipeline Behavior support types ---

/// <summary>
/// A generic pipeline behavior that can be registered as an open generic
/// and should apply to ALL request/response types.
/// </summary>
public class GenericPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly List<string> _log;

    public GenericPipelineBehavior(List<string> log)
    {
        _log = log;
    }

    public async ValueTask<TResponse> Handle(TRequest input, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken = default)
    {
        _log.Add($"GenericBehavior<{typeof(TRequest).Name},{typeof(TResponse).Name}>-before");
        var result = await next();
        _log.Add($"GenericBehavior<{typeof(TRequest).Name},{typeof(TResponse).Name}>-after");
        return result;
    }
}

public record OpenGenericQueryA(int Value)
    : IRequest<int>;

public class OpenGenericQueryAHandler : IRequestHandler<OpenGenericQueryA, int>
{
    public int InvocationCount { get; private set; }

    public ValueTask<int> Handle(OpenGenericQueryA query, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        return new ValueTask<int>(Task.FromResult(query.Value * 3));
    }
}

public record OpenGenericQueryB(string Text)
    : IRequest<string>;

public class OpenGenericQueryBHandler : IRequestHandler<OpenGenericQueryB, string>
{
    public int InvocationCount { get; private set; }

    public ValueTask<string> Handle(OpenGenericQueryB query, CancellationToken cancellationToken = default)
    {
        InvocationCount++;
        return new ValueTask<string>(Task.FromResult($"echo:{query.Text}"));
    }
}
