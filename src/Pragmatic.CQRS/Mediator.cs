using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Pragmatic.CQRS;

public class Mediator(IServiceProvider provider, MediatorCacheMap cacheMap, ILogger<Mediator>? logger = null)
    : IMediator
{
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        try
        {
            var cacheEntry = cacheMap.GetOrAdd(requestType, responseType);

            // Transient lifespan here - can't cache and re-use.
            var handler = provider.GetService(cacheEntry.Handler.Type);
            var behaviors = provider.GetServices(cacheEntry.Behaviour.Type).Reverse();

            if (handler == null)
            {
                throw new CqrsException(
                    $"No handler registered implementing IRequestHandler<{requestType.Name}, {responseType.Name}>.",
                    cacheEntry.Handler.Type);
            }

            RequestHandlerDelegate<TResponse> handlerDelegate = () =>
            {
                var executionHandler = (Func<object, object, object, object>)cacheEntry.Handler.Method;
                var result = executionHandler(handler, request, cancellationToken)
                    ?? throw new CqrsException($"Cannot resolve handler method for Handler: {cacheEntry.Handler.Type.FullName}", cacheEntry.Handler.Type);

                return (Task<TResponse>)result;
            };

            foreach (var behavior in behaviors)
            {
                if (behavior == null)
                    continue;

                var next = handlerDelegate;
                handlerDelegate = () =>
                {
                    var executionHandler = (Func<object, object, object, object, object>)cacheEntry.Behaviour.Method;
                    var result = executionHandler(behavior, request, next, cancellationToken)
                        ?? throw new CqrsException($"Cannot resolve handler method for Behaviour: {cacheEntry.Behaviour.Type.FullName}", cacheEntry.Behaviour.Type);

                    return (Task<TResponse>)result;
                };
            }

            return await handlerDelegate();
        }
        catch (TargetInvocationException ex)
        {
            var inner = ex.InnerException;
            if (inner is OperationCanceledException oce)
            {
                throw oce;  // preserve exact cancellation semantics
            }

#pragma warning disable S6667
            logger?.LogError(inner ?? ex, "Exception processing request '{RequestType}<{ResponseType}>'", requestType.FullName, responseType.FullName);
#pragma warning restore S6667

            ExceptionDispatchInfo.Capture(inner ?? ex).Throw();
            return default; // Not reached
        }
    }

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        try
        {
            var cacheEntry = cacheMap.GetOrAdd(requestType);

            // Transient lifespan here - can't cache and re-use.
            var handler = provider.GetService(cacheEntry.Handler.Type);
            var behaviors = provider.GetServices(cacheEntry.Behaviour.Type).Reverse();

            if (handler == null)
            {
                throw new CqrsException(
                    $"No handler registered implementing IRequestHandler<{requestType.Name}>.",
                    cacheEntry.Handler.Type);
            }

            RequestHandlerDelegate handlerDelegate = () =>
            {
                var executionHandler = (Func<object, object, object, object>)cacheEntry.Handler.Method;
                var result = executionHandler(handler, request, cancellationToken)
                    ?? throw new CqrsException($"Cannot resolve handler method for Handler: {cacheEntry.Handler.Type.FullName}", cacheEntry.Handler.Type);

                return (Task)result;
            };

            foreach (var behavior in behaviors)
            {
                if (behavior == null)
                    continue;

                var next = handlerDelegate;
                handlerDelegate = () =>
                {
                    var executionHandler = (Func<object, object, object, object, object>)cacheEntry.Behaviour.Method;
                    var result = executionHandler(behavior, request, next, cancellationToken)
                        ?? throw new CqrsException($"Cannot resolve handler method for Behaviour: {cacheEntry.Behaviour.Type.FullName}", cacheEntry.Behaviour.Type);

                    return (Task)result;
                };
            }

            await handlerDelegate();
        }
        catch (TargetInvocationException ex)
        {
            // Unpack the reflection error here.
            var inner = ex.InnerException;
            if (inner is OperationCanceledException oce)
            {
                throw oce;  // preserve exact cancellation semantics
            }

#pragma warning disable S6667
            logger?.LogError(inner ?? ex, "Exception processing request '{RequestType}'", requestType.FullName);
#pragma warning restore S6667

            ExceptionDispatchInfo.Capture(inner ?? ex).Throw();
        }
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();

        var handlerMap = cacheMap.GetOrAddNotification(notificationType);

        // Get all notification handlers (multiple handlers per notification supported)
        var handlers = provider.GetServices(handlerMap.Type).ToArray();

        if (handlers.Length == 0)
        {
            logger?.LogDebug("No handlers registered for notification type '{NotificationType}'. Notification will be silently dropped.", notificationType.FullName);
        }

        var tasks = handlers.Select(async handler =>
        {
            if (handler == null) return;

            try
            {
                var executionHandler = (Func<object, object, object, object>)handlerMap.Method;
                var result = executionHandler(handler, notification, cancellationToken)
                    ?? throw new CqrsException($"Cannot resolve handler method for Handler: {handlerMap.Type.FullName}", handlerMap.Type);

                await (Task)result;
            }
            catch (OperationCanceledException)
            {
                throw;  // preserve cancellation semantics
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Exception occurred while notifying handler '{Handler}' for notification '{Notification}'", handlerMap.Type.FullName, notificationType.FullName);
            }
        }).ToArray();

        await Task.WhenAll(tasks);
    }
}
