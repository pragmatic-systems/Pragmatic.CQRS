using Microsoft.Extensions.DependencyInjection;

namespace Pragmatic.CQRS;

public class SendDispatcher<TRequest, TResponse> : ISendDispatcher<TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Invoke(IServiceProvider provider, IRequest<TResponse> request, CancellationToken cancellationToken = default)
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

        RequestHandlerDelegate<TResponse> handlerDelegate = () => handler.Handle((TRequest)request, cancellationToken);

        foreach (var behavior in behaviors)
        {
            if (behavior == null)
                continue;

            var next = handlerDelegate;
            handlerDelegate = () => behavior.Handle((TRequest)request, next, cancellationToken);
        }

        return await handlerDelegate();
    }
}

public class SendDispatcher<TRequest> : ISendDispatcher
    where TRequest : IRequest
{
    public async Task Invoke(IServiceProvider provider, IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Transient lifespan here - can't cache and re-use.
        var handler = provider.GetService<IRequestHandler<TRequest>>();
        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, Unit>>().Reverse();

        if (handler == null)
        {
            throw new CqrsException(
                $"No handler registered implementing IRequestHandler<{typeof(TRequest).Name}>.");
        }

        RequestHandlerDelegate<Unit> handlerDelegate = async () =>
        {
            await handler.Handle((TRequest)request, cancellationToken);
            return Unit.Instance;
        };

        foreach (var behavior in behaviors)
        {
            if (behavior == null)
                continue;

            var next = handlerDelegate;
            handlerDelegate = () => behavior.Handle((TRequest)request, next, cancellationToken);
        }

        await handlerDelegate();
    }
}
