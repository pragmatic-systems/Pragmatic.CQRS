# Pragmatic.CQRS

A lightweight CQRS mediator implementation for .NET that mirrors the MediatR interfaces. Built for simplicity, performance, and clean separation of concerns.

## Features

- **Request/Response Handlers** — Send a command or query and receive a typed result.
- **Void (Fire-and-Forget) Handlers** — Send a request without expecting a return value.
- **Notification Fan-Out** — Publish an event to multiple handlers concurrently.
- **Pipeline Behaviors** — Cross-cutting middleware (logging, validation, timing) applied in reverse registration order (LIFO).
- **Auto-Registration** — Scan assemblies and register all handlers automatically.
- **Compiled Expression Dispatch** — Reflection is cached and compiled into delegates for performance.
- **ValueTask Support** — All handler and behavior interfaces return `ValueTask`/`ValueTask<T>` to avoid `Task` allocation for synchronously-completed operations.
- **Cancellation Support** — Full `CancellationToken` propagation through the pipeline.

## Installation

```bash
dotnet add package Pragmatic.CQRS
```

## Quick Start

### 1. Register the Mediator

```csharp
services.AddCqrs(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        new[] { typeof(Program).Assembly });
});
```

This registers `IMediator` as transient and auto-discovers all handler implementations in the specified assemblies.

### 2. Define a Request and Handler

```csharp
public record GetUserIdRequest(Guid Id) : IRequest<int>;

public class GetUserIdHandler : IRequestHandler<GetUserIdRequest, int>
{
    public ValueTask<int> Handle(GetUserIdRequest request, CancellationToken cancellationToken = default)
    {
        return new ValueTask<int>(42);
    }
}
```

### 3. Send the Request

```csharp
public class MyService
{
    private readonly IMediator _mediator;

    public MyService(IMediator mediator) => _mediator = mediator;

    public async Task<int> GetUserIdAsync(Guid id)
    {
        return await _mediator.Send(new GetUserIdRequest(id));
    }
}
```

### Fire-and-Forget (Void) Requests

```csharp
public record DeleteUserRequest(Guid Id) : IRequest;

public class DeleteUserHandler : IRequestHandler<DeleteUserRequest>
{
    public ValueTask Handle(DeleteUserRequest request, CancellationToken cancellationToken = default)
    {
        // delete logic
        return ValueTask.CompletedTask;
    }
}

// usage
await _mediator.Send(new DeleteUserRequest(userId));
```

### Notifications (Event Fan-Out)

```csharp
public record UserCreated : INotification
{
    public Guid UserId { get; }
    public UserCreated(Guid userId) => UserId = userId;
}

public class SendWelcomeEmailHandler : INotificationHandler<UserCreated>
{
    public ValueTask Handle(UserCreated notification, CancellationToken cancellationToken = default)
    {
        // send email
        return ValueTask.CompletedTask;
    }
}

public class AuditLogHandler : INotificationHandler<UserCreated>
{
    public ValueTask Handle(UserCreated notification, CancellationToken cancellationToken = default)
    {
        // write audit log
        return ValueTask.CompletedTask;
    }
}

// usage - all handlers are invoked concurrently
await _mediator.Publish(new UserCreated(userId));
```

### Pipeline Behaviors

Register pipeline behaviors separately. They wrap handlers in reverse registration order (LIFO).

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        => _logger = logger;

    public async ValueTask<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {RequestType}", typeof(TRequest).Name);
        var response = await next();
        _logger.LogInformation("Handled {RequestType}", typeof(TRequest).Name);
        return response;
    }
}

// register manually
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

## ValueTask Performance

All handler and behavior interfaces return `ValueTask`/`ValueTask<T>`. When a handler completes synchronously (e.g., returning a cached value), using `new ValueTask<T>(result)` or `ValueTask.CompletedTask` avoids allocating a `Task` object entirely. This reduces GC pressure in high-throughput scenarios.

```csharp
// Zero allocation — no Task created
public ValueTask<int> Handle(MyQuery request, CancellationToken cancellationToken = default)
{
    return new ValueTask<int>(_cache[request.Id]);
}

// Async operations still work naturally
public async ValueTask<int> Handle(MyQuery request, CancellationToken cancellationToken = default)
{
    var result = await _db.QueryAsync(request.Id, cancellationToken);
    return result;
}
```

## API Reference

| Interface | Purpose |
|-----------|---------|
| `IMediator` | The central mediator — `Send<TResponse>`, `Send`, `Publish` |
| `IRequest` | Marker for void requests (no return value) |
| `IRequest<T>` | Marker for requests expecting a typed response |
| `IRequestHandler<TRequest>` | Handles void requests (returns `ValueTask`) |
| `IRequestHandler<TRequest, TResult>` | Handles typed requests (returns `ValueTask<TResult>`) |
| `INotification` | Marker for publish/subscribe events |
| `INotificationHandler<TNotification>` | Handles a notification (multiple handlers supported, returns `ValueTask`) |
| `IPipelineBehavior<TRequest, TResponse>` | Middleware for typed requests (returns `ValueTask<TResponse>`) |
| `IPipelineBehavior<TRequest>` | Middleware for void requests (returns `ValueTask`) |

## License

MIT — see [LICENSE](https://github.com/pragmatic-systems/Pragmatic.CQRS/blob/main/LICENSE.txt)
