using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Pragmatic.CQRS;

public class MediatorCacheMap
{
    public sealed record MediatorMap(Type Type, Delegate Method);

    public sealed record MediatorCacheEntry(MediatorMap Handler, MediatorMap Behaviour);

    private readonly ConcurrentDictionary<(Type, Type?), MediatorCacheEntry> _cache = new();
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

            var v1Dispatcher = (Delegate?)closedMethod.Invoke(this, null)
                ?? throw new CqrsException($"Failed to create dispatcher for {requestType.Name}<{responseType.Name}>", requestType);

            return WrapV1InV2Delegate<TResponse>(v1Dispatcher);
        });
    }

    public SendDispatcherDelegate<TRequest, TResponse> BuildDispatcher<TRequest, TResponse>()
             where TRequest : IRequest<TResponse>
    {
        return SendDispatcher<TRequest, TResponse>.Create();
    }

    public MediatorCacheEntry GetOrAdd(Type requestType)
    {
        return _cache.GetOrAdd((requestType, null), _ =>
        {
            var handlerMap = GetHandlerMap(requestType);
            var behaviourMap = GetBehaviourMap(requestType);

            return new MediatorCacheEntry(
                handlerMap,
                behaviourMap);
        });
    }

    public MediatorMap GetOrAddNotification(Type notificationType)
    {
        return _notificationCache.GetOrAdd(notificationType, _ =>
        {
            return GetNotificationHandlerMap(notificationType);
        });
    }

    private static MediatorMap GetHandlerMap(Type requestType)
    {
        var handlerType = typeof(IRequestHandler<>).MakeGenericType(requestType);
        var handlerParamObj = Expression.Parameter(typeof(object), "handlerObj");
        var requestParamObj = Expression.Parameter(typeof(object), "requestObj");
        var ctParamObj = Expression.Parameter(typeof(object), "ctObj");

        var handleMethod = handlerType.GetMethod("Handle", new[] { requestType, typeof(CancellationToken) })
            ?? throw new CqrsException($"Cannot resolve Handle method for Handler: {handlerType.FullName}", handlerType);

        var handlerExpr = Expression.Convert(handlerParamObj, handlerType);
        var requestExpr = Expression.Convert(requestParamObj, requestType);
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

    private static MediatorMap GetBehaviourMap(Type requestType)
    {
        var responseType = typeof(Unit);
        var behaviourType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);
        var nextType = typeof(RequestHandlerDelegate<>).MakeGenericType(responseType);

        var behaviourParamObj = Expression.Parameter(typeof(object), "behaviourObj");
        var inputParamObj = Expression.Parameter(typeof(object), "inputObj");
        var requestNextObj = Expression.Parameter(typeof(object), "nextObj");
        var ctParamObj = Expression.Parameter(typeof(object), "ctObj");

        var behaviourMethod = behaviourType.GetMethod("Handle", new Type[] { requestType, nextType, typeof(CancellationToken) })
            ?? throw new CqrsException($"Cannot resolve Handle method for Behaviour: {behaviourType.FullName}", behaviourType);

        var behaviourExpr = Expression.Convert(behaviourParamObj, behaviourType);
        var requestExpr = Expression.Convert(inputParamObj, requestType);
        var nextExpr = Expression.Convert(requestNextObj, nextType);
        var ctExpr = Expression.Convert(ctParamObj, typeof(CancellationToken));

        var callExpr = Expression.Call(behaviourExpr, behaviourMethod, requestExpr, nextExpr, ctExpr);

        var lambdaExpr = Expression.Lambda<Func<object, object, object, object, object>>(
            callExpr,
            behaviourParamObj,
            inputParamObj,
            requestNextObj,
            ctParamObj);

        var handlerDelegate = lambdaExpr.Compile();

        return new MediatorMap(behaviourType, handlerDelegate);
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

    private static SendDispatcherDelegateV2<TResponse> WrapV1InV2Delegate<TResponse>(Delegate v1Dispatcher)
    {
        return (provider, request, ct) =>
        {
            var result = (Task<TResponse>)v1Dispatcher.DynamicInvoke(provider, request, ct)!;
            return result;
        };
    }
}
