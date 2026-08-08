using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Pragmatic.CQRS;

public class MediatorCacheMap
{
    public sealed record MediatorMap(Type Type, Delegate Method);

    public sealed record MediatorCacheEntry(MediatorMap Handler, MediatorMap Behaviour);

    private readonly ConcurrentDictionary<Type, MediatorMap> _notificationCache = new();
    private readonly ConcurrentDictionary<(Type RequestType, Type ResponseType), object> _dispatcherCache = new();

    public ISendDispatcher<TResponse> GetOrAddDispatcher<TResponse>(Type requestType, Type responseType)
    {
        return (ISendDispatcher<TResponse>)_dispatcherCache.GetOrAdd((requestType, responseType), _ =>
        {
            var dispatcherType = typeof(SendDispatcher<,>).MakeGenericType(requestType, responseType);
            return Activator.CreateInstance(dispatcherType)
                ?? throw new CqrsException($"Failed to create dispatcher for {requestType.Name}<{responseType.Name}>", requestType);
        });
    }

    public ISendDispatcher GetOrAddDispatcherVoid(Type requestType)
    {
        return (ISendDispatcher)_dispatcherCache.GetOrAdd((requestType, typeof(Unit)), _ =>
        {
            var dispatcherType = typeof(SendDispatcher<>).MakeGenericType(requestType);
            return Activator.CreateInstance(dispatcherType)
                ?? throw new CqrsException($"Failed to create void dispatcher for {requestType.Name}", requestType);
        });
    }

    public MediatorMap GetOrAddNotification(Type notificationType)
    {
        return _notificationCache.GetOrAdd(notificationType, _ =>
        {
            return GetNotificationHandlerMap(notificationType);
        });
    }

    private static MediatorMap GetNotificationHandlerMap(Type notificationType)
    {
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);
        var handlerParamObj = Expression.Parameter(typeof(object), "handlerObj");
        var requestParamObj = Expression.Parameter(typeof(object), "notificationObj");
        var ctParamObj = Expression.Parameter(typeof(object), "ctObj");

        var handleMethod = handlerType.GetMethod("Handle", new[] { notificationType, typeof(CancellationToken) })
            ?? throw new CqrsException($"Cannot resolve Handle method for Handler: {handlerType.FullName}", handlerType);

        var handlerExpr = Expression.Convert(handlerParamObj, handlerType);
        var requestExpr = Expression.Convert(requestParamObj, notificationType);
        var ctExpr = Expression.Convert(ctParamObj, typeof(CancellationToken));

        var callExpr = Expression.Call(handlerExpr, handleMethod, requestExpr, ctExpr);

        var lambdaExpr = Expression.Lambda<Func<object, object, object, object>>(
            callExpr,
            handlerParamObj,
            requestParamObj,
            ctParamObj);

        var handlerDelegate = lambdaExpr.Compile();

        return new MediatorMap(handlerType, handlerDelegate);
    }
}

public interface ISendDispatcher<TResponse>
{
    Task<TResponse> Invoke(IServiceProvider provider, IRequest<TResponse> request, CancellationToken cancellationToken);
}

public interface ISendDispatcher
{
    Task Invoke(IServiceProvider provider, IRequest request, CancellationToken cancellationToken);
}
