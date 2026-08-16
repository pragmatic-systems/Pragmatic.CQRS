using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Pragmatic.CQRS;

public class MediatorCacheMap
{
    private readonly ConcurrentDictionary<Type, object> _notificationDispatcherCache = new();
    private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), object> _dispatcherCache = new();

    public SendDispatcherBase<TResponse> GetOrAddDispatcher<TResponse>(Type requestType, Type responseType)
    {
        return (SendDispatcherBase<TResponse>)_dispatcherCache.GetOrAdd((requestType, responseType), _ =>
        {
            var dispatcherType = typeof(SendDispatcher<,>).MakeGenericType(requestType, responseType);
            return Activator.CreateInstance(dispatcherType)
                ?? throw new CqrsException($"Failed to create dispatcher for {requestType.Name}<{responseType.Name}>", requestType);
        });
    }

    public SendDispatcherBase GetOrAddDispatcherVoid(Type requestType)
    {
        return (SendDispatcherBase)_dispatcherCache.GetOrAdd((requestType, typeof(Unit)), _ =>
        {
            var dispatcherType = typeof(SendDispatcher<>).MakeGenericType(requestType);
            return Activator.CreateInstance(dispatcherType)
                ?? throw new CqrsException($"Failed to create void dispatcher for {requestType.Name}", requestType);
        });
    }

    public NotificationDispatcherBase GetOrAddNotificationDispatcher(Type notificationType, ILogger<Mediator>? logger = null)
    {
        return (NotificationDispatcherBase)_notificationDispatcherCache.GetOrAdd(notificationType, _ =>
        {
            var dispatcherType = typeof(NotificationDispatcher<>).MakeGenericType(notificationType);
            return Activator.CreateInstance(dispatcherType, new object?[] { logger })
                ?? throw new CqrsException($"Failed to create notification dispatcher for {notificationType.Name}", notificationType);
        });
    }
}
