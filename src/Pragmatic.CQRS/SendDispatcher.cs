using Microsoft.Extensions.DependencyInjection;

namespace Pragmatic.CQRS;

public class SendDispatcher<TRequest, TResponse> : SendDispatcherBase<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override async Task<TResponse> Invoke(IServiceProvider provider, IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Transient lifespan here - can't cache and re-use.
        var handler = provider.GetService<IRequestHandler<TRequest, TResponse>>();
        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, TResponse>>().ToArray();

        if (handler == null)
        {
            throw new CqrsException(
                $"No handler registered implementing IRequestHandler<{typeof(TRequest).Name}, {typeof(TResponse).Name}>.");
        }

        RequestHandlerDelegate<TResponse> next = () => handler.Handle((TRequest)request, cancellationToken);

        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            if (behavior == null)
                continue;

            var capturedNext = next;
            next = () => behavior.Handle((TRequest)request, capturedNext, cancellationToken);
        }

        return await next();
    }
}

public class SendDispatcher<TRequest> : SendDispatcherBaseVoid
    where TRequest : IRequest
{
    public override async Task Invoke(IServiceProvider provider, IRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Transient lifespan here - can't cache and re-use.
        var handler = provider.GetService<IRequestHandler<TRequest>>();
        var behaviors = provider.GetServices<IPipelineBehavior<TRequest, Unit>>().ToArray();

        if (handler == null)
        {
            throw new CqrsException(
                $"No handler registered implementing IRequestHandler<{typeof(TRequest).Name}>.");
        }

        RequestHandlerDelegate<Unit> next = async () =>
        {
            await handler.Handle((TRequest)request, cancellationToken);
            return Unit.Instance;
        };

        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            if (behavior == null)
                continue;

            var capturedNext = next;
            next = () => behavior.Handle((TRequest)request, capturedNext, cancellationToken);
        }

        await next();
    }
}
