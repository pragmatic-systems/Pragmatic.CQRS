using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Pragmatic.CQRS;

public class Mediator(IServiceProvider provider, MediatorCacheMap cacheMap, ILogger<Mediator>? logger = null)
    : IMediator
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        var dispatcher = cacheMap.GetOrAddDispatcher<TResponse>(requestType, responseType);
        return dispatcher.Invoke(provider, request, cancellationToken);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();

        var dispatcher = cacheMap.GetOrAddDispatcherVoid(requestType);
        return dispatcher.Invoke(provider, request, cancellationToken);
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
