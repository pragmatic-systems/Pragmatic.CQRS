# Pragmatic.CQRS

A lightweight, MIT-licensed CQRS mediator for .NET with MediatR-inspired interfaces.

A small implementation of the core mediator patterns — request/response handlers, void (fire-and-forget) handlers, notification fan-out, and pipeline behaviors — with a small code footprint, minimal dependencies, and minimal overhead.

## Status

| Measure | Level |
|:-|:-|
| [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) | [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=coverage)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) |
| [![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) | [![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) |
| [![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) | [![Bugs](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=bugs)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) |
| [![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) | [![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) |

## Features

- **Request/Response Handlers** — send a command or query and receive a typed result.
- **Void (Fire-and-Forget) Handlers** — send a request without expecting a return value.
- **Notification Fan-Out** — publish an event; all registered handlers run concurrently, with per-handler failures isolated and logged.
- **Pipeline Behaviors** — cross-cutting middleware (logging, validation, timing) that run in the order they are registered with the container.
- **Auto-Registration** — scan assemblies and register all handlers automatically.
- **Cached Generic Dispatch** — each request type is compiled into a generic dispatcher exactly once and cached; no per-call reflection.
- **Cancellation Support** — `CancellationToken` propagation through the whole pipeline.
- **Lean Dependencies** — only `Microsoft.Extensions.DependencyInjection.Abstractions` and `Microsoft.Extensions.Logging.Abstractions`.

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
        => Task.FromResult(42);
}
```

### 3. Send the Request

```csharp
public class MyService
{
    private readonly IMediator _mediator;

    public MyService(IMediator mediator) => _mediator = mediator;

    public Task<int> GetUserIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _mediator.Send(new GetUserIdRequest(id), cancellationToken);
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

// usage — all handlers for the notification type are invoked concurrently
await _mediator.Publish(new UserCreated(userId));
```

A handler that throws does not fail the publish or its siblings — the exception is logged and the remaining handlers still complete. `OperationCanceledException` is preserved and propagated.

### Pipeline Behaviors

Behaviors are not auto-registered — register them per request type, after `AddCqrs`. They wrap handlers in reverse registration order (LIFO):

```csharp
public class LoggingBehavior<TResponse, TResponse> : IPipelineBehavior<TResponse, TResponse>
{
    private readonly ILogger<LoggingBehavior<TResponse, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TResponse, TResponse>> logger)
        => _logger = logger;

    public async Task<TResponse> Handle(
        TResponse request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Handling {RequestType}", typeof(TResponse).Name);
        var response = await next(cancellationToken);
        _logger.LogInformation("Handled {RequestType}", typeof(TResponse).Name);
        return response;
    }
}

// register for a specific request type
services.AddScoped<IPipelineBehavior<GetUserIdRequest, int>, LoggingBehavior<GetUserIdRequest, int>>();
```

Behaviors run in the order they are registered with the container (the first registered runs first). Open-generic registration applies a behavior to every matching request type:

```csharp
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
```

Void requests use the `Unit` result type in their behavior signature:

```csharp
services.AddScoped<IPipelineBehavior<DeleteUserRequest, Unit>, LoggingBehavior<DeleteUserRequest, Unit>>();
```

## API Reference

| Type | Purpose |
|------|---------|
| `IMediator` | The central mediator — `Send<TResponse>`, `Send<TResponse>`, `Publish` |
| `IRequest` | Marker for void requests (no return value) |
| `IRequest<TResult>` | Marker for requests expecting a typed response |
| `IRequestHandler<TResponse>` | Handles void requests — `Task Handle(TResponse, CancellationToken)` |
| `IRequestHandler<TResponse, TResult>` | Handles typed requests — `Task<TResult> Handle(TResponse, CancellationToken)` |
| `INotification` | Marker for publish/subscribe events |
| `INotificationHandler<TNotification>` | Handles a notification — multiple handlers supported |
| `IPipelineBehavior<TResponse, TResponse>` | Middleware for typed requests (use `IPipelineBehavior<TResponse, Unit>` for void requests) |
| `RequestHandlerDelegate` / `RequestHandlerDelegate<TResponse>` | The `next` delegate passed to pipeline behaviors |
| `Unit` | Void result marker used in void pipeline behavior signatures |
| `CqrsException` | Mediator errors (e.g. no handler registered for a request) |

## Behavioral Notes

- `Send` uses exactly one handler per request (the first one registered for that request type); `Publish` fans out to all registered handlers.
- Handler exceptions propagate to the caller unwrapped — the mediator does not wrap them.
- Handlers and behaviors are resolved from the DI container on every dispatch, so transient lifetimes are safe.
- Dispatchers are cached per (request type, response type) after first use; reflection is only involved once per type.
- Target framework: `net8.0`.

## Building Locally

```bash
# Build and test
dotnet cake --Target=BuildAndTest

# Build and benchmark
dotnet cake --Target=BuildAndBenchmark

# Pack and push to a local NuGet folder
dotnet cake --Target=LocalNugetPackAndPush --NuGetSource="c:\package-source" --NuGetApiKey="key"
```

## License

MIT — see [LICENSE.txt](LICENSE.txt)
