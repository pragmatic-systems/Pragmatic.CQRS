using Microsoft.Extensions.DependencyInjection;

namespace Pragmatic.CQRS;

public class SendDispatcher<TRequest, TResponse> : SendDispatcherBase<TResponse>
    where TRequest : IRequest<TResponse>
{
    public override Task<TResponse> Invoke(IServiceProvider provider, IRequest<TResponse> request, CancellationToken cancellationToken = default)
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

        var typedRequest = (TRequest)request;

        RequestHandlerDelegate<TResponse> next = ct => handler.Handle(typedRequest, ct);

        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            if (behavior == null)
                continue;

            var capturedNext = next;
            next = ct => behavior.Handle(typedRequest, capturedNext, ct);
        }

        return next(cancellationToken);
    }
}

public class SendDispatcher<TRequest> : SendDispatcherBase
    where TRequest : IRequest
{
    public override Task Invoke(IServiceProvider provider, IRequest request, CancellationToken cancellationToken = default)
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

        var typedRequest = (TRequest)request;

        RequestHandlerDelegate<Unit> next = async ct =>
        {
            // NOTE: The await here is nescessary to execute the Void handler and return Unit after complete.
            await handler.Handle(typedRequest, ct);
            return Unit.Instance;
        };

        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            if (behavior == null)
                continue;

            var capturedNext = next;
            next = ct => behavior.Handle(typedRequest, capturedNext, ct);
        }

        return next(cancellationToken);
    }
}
