using System.Reflection;
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
            // TODO: Make resilient.
            var genericMethod = typeof(Mediator)
                .GetMethods()
                .Where(m => m.Name == nameof(Send))
                .Skip(1)
                .First();

            var closedMethod = genericMethod.MakeGenericMethod(requestType, responseType);

            return await (Task<TResponse>)closedMethod.Invoke(this, new object[] { request, cancellationToken });
        }
        catch (OperationCanceledException)
        {
            throw;  // preserve cancellation semantics
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Exception processing request '{RequestType}<{ResponseType}>'", requestType.FullName, responseType.FullName);
            throw;
        }
    }

    public async Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var dispatcher = cacheMap.GetOrAddDispatcherX<TRequest, TResponse>();

            return await dispatcher(provider, request, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;  // preserve cancellation semantics
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Exception processing request '{RequestType}<{ResponseType}>'", typeof(TRequest).FullName, typeof(TResponse).FullName);
            throw;
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

            RequestHandlerDelegate<Unit> handlerDelegate = async () =>
            {
                var executionHandler = (Func<object, object, object, object>)cacheEntry.Handler.Method;
                var result = executionHandler(handler, request, cancellationToken)
                    ?? throw new CqrsException($"Cannot resolve handler method for Handler: {cacheEntry.Handler.Type.FullName}", cacheEntry.Handler.Type);

                await (Task)result;
                return Unit.Instance;
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

                    return (Task<Unit>)result;
                };
            }

            await handlerDelegate();
        }
        catch (OperationCanceledException)
        {
            throw;  // preserve cancellation semantics
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Exception processing request '{RequestType}'", requestType.FullName);
            throw;
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

public delegate Task<TResponse> SendDispatcherDelegate<TRequest, TResponse>(
    IServiceProvider provider,
    TRequest request,
    CancellationToken cancellationToken)
    where TRequest : IRequest<TResponse>;

public static class SendDispatcher<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public static SendDispatcherDelegate<TRequest, TResponse> Create()
    {
        return Send;
    }

    public static async Task<TResponse> Send(IServiceProvider provider, TRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Transient lifespan here - can't cache and re-use.
        var handler = provider.GetService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().Reverse();

        if (handler == null)
        {
            throw new CqrsException(
                $"No handler registered implementing IRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}>.");
        }

        RequestHandlerDelegate<TResponse> handlerDelegate = () => handler.Handle(request, cancellationToken);

        foreach (var behavior in behaviors)
        {
            if (behavior == null)
                continue;

            var next = handlerDelegate;
            handlerDelegate = () => behavior.Handle(request, next, cancellationToken);
        }

        return await handlerDelegate();
    }
}
