# Pragmatic.CQRS

A lightweight CQRS mediator implementation for .NET that mirrors the MediatR interfaces. Built for simplicity, performance, and clean separation of concerns.

## Features

- **Request/Response Handlers** — Send a command or query and receive a typed result.
- **Void (Fire-and-Forget) Handlers** — Send a request without expecting a return value.
- **Notification Fan-Out** — Publish an event to multiple handlers concurrently.
- **Pipeline Behaviors** — Cross-cutting middleware (logging, validation, timing) applied in reverse registration order (LIFO).
- **Auto-Registration** — Scan assemblies and register all handlers automatically.
- **Compiled Expression Dispatch** — Reflection is cached and compiled into delegates for performance.
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
    public Task<int> Handle(GetUserIdRequest request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(42);
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
    public Task Handle(DeleteUserRequest request, CancellationToken cancellationToken = default)
    {
        // delete logic
        return Task.CompletedTask;
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
    public Task Handle(UserCreated notification, CancellationToken cancellationToken = default)
    {
        // send email
        return Task.CompletedTask;
    }
}

public class AuditLogHandler : INotificationHandler<UserCreated>
{
    public Task Handle(UserCreated notification, CancellationToken cancellationToken = default)
    {
        // write audit log
        return Task.CompletedTask;
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

    public async Task<TResponse> Handle(
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

## API Reference

| Interface | Purpose |
|-----------|---------|
| `IMediator` | The central mediator — `Send<TResponse>`, `Send`, `Publish` |
| `IRequest<T>` | Marker for void requests (no return value) |
| `IRequest<T, TResult>` | Marker for requests expecting a typed response |
| `IRequestHandler<TRequest>` | Handles void requests |
| `IRequestHandler<TRequest, TResult>` | Handles typed requests |
| `INotification` | Marker for publish/subscribe events |
| `INotificationHandler<TNotification>` | Handles a notification (multiple handlers supported) |
| `IPipelineBehavior<TRequest, TResponse>` | Middleware for typed requests |
| `IPipelineBehavior<TRequest>` | Middleware for void requests |

## License

MIT — see [LICENSE](https://github.com/pragmatic-systems/Pragmatic.CQRS/blob/main/LICENSE.txt)
