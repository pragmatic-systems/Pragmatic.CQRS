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
    private readonly ConcurrentDictionary<(Type, Type), Delegate> _dispatcherCache = new();

    public SendDispatcherDelegateV2<TResponse> GetOrAddDispatcher<TResponse>(IRequest<TResponse> request)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResponse);

        return (SendDispatcherDelegateV2<TResponse>)_dispatcherCache.GetOrAdd((requestType, responseType), _ =>
        {
            var genericMethod = typeof(MediatorCacheMap)
                .GetMethod(nameof(BuildDispatcher), BindingFlags.Public | BindingFlags.Instance)!;

            var closedMethod = genericMethod.MakeGenericMethod(requestType, responseType);

            return (Delegate)closedMethod.Invoke(this, null)!
                ?? throw new CqrsException($"Failed to create dispatcher for {requestType.Name}<{responseType.Name}>", requestType);
        });
    }

    public SendDispatcherDelegateV2<TResponse> BuildDispatcher<TRequest, TResponse>()
             where TRequest : IRequest<TResponse>
    {
        return (provider, request, ct) =>
            SendDispatcher<TRequest, TResponse>.Send(provider, (TRequest)request, ct);
    }

    public SendDispatcherDelegateV2Void GetOrAddDispatcherVoid(IRequest request)
    {
        var requestType = request.GetType();

        return (SendDispatcherDelegateV2Void)_dispatcherCache.GetOrAdd((requestType, typeof(Unit)), _ =>
        {
            var genericMethod = typeof(MediatorCacheMap)
                .GetMethod(nameof(BuildDispatcherVoid), BindingFlags.Public | BindingFlags.Instance)!;

            var closedMethod = genericMethod.MakeGenericMethod(requestType);

            return (Delegate)closedMethod.Invoke(this, null)!
                ?? throw new CqrsException($"Failed to create void dispatcher for {requestType.Name}", requestType);
        });
    }

    public SendDispatcherDelegateV2Void BuildDispatcherVoid<TRequest>()
        where TRequest : IRequest
    {
        return (provider, request, ct) =>
            SendDispatcherVoid<TRequest>.Send(provider, (TRequest)request, ct);
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
