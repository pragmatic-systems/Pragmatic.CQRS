namespace Pragmatic.CQRS;

public interface IMediator
{
    Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken);

    Task Send<TRequest>(TRequest request, CancellationToken cancellationToken)
        where TRequest : IRequest;

    Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken)
        where TNotification : INotification;
}
