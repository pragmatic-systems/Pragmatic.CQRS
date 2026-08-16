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

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();

        var dispatcher = cacheMap.GetOrAddNotificationDispatcher(notificationType, logger);
        return dispatcher.Invoke(provider, notification, cancellationToken);
    }
}
