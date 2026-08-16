namespace Pragmatic.CQRS;

public abstract class SendDispatcherBase<TResponse>
{
    public abstract Task<TResponse> Invoke(IServiceProvider provider, IRequest<TResponse> request, CancellationToken cancellationToken);
}

public abstract class SendDispatcherBase
{
    public abstract Task Invoke(IServiceProvider provider, IRequest request, CancellationToken cancellationToken);
}

public abstract class NotificationDispatcherBase
{
    public abstract Task Invoke(IServiceProvider provider, INotification notification, CancellationToken cancellationToken);
}
