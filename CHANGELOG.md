# Changelog

## [1.0.0] - 2025-07-20

### Features
- Request/Response handlers (`IRequest<TResponse>`, `IRequestHandler<TRequest, TResult>`)
- Void (fire-and-forget) handlers (`IRequest`, `IRequestHandler<TRequest>`)
- Notification/broadcast fan-out (`INotification`, `INotificationHandler<TNotification>`, `Publish`)
- Pipeline behavior middleware with reverse-order (LIFO) execution
- Auto-registration of handlers from assemblies via `AddCqrs`
- Compiled expression-based dispatch for performance
- Full cancellation token support
