# Pragmatic.CQRS

A lightweight CQRS mediator implementation for .NET that mirrors the MediatR interfaces. Built for simplicity, performance, and clean separation of concerns.

## Status

| Measure | Level |
|:-|:-|
| [![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) | [![Coverage](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=coverage)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) |
| [![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) | [![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) |
| [![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) | [![Bugs](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=bugs)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) |
| [![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) | [![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS) |

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
