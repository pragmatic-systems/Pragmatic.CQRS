namespace Pragmatic.CQRS;

public interface IBaseRequestHandler { }

public interface IRequestHandler<in TRequest> : IBaseRequestHandler
    where TRequest : IRequest
{
    ValueTask Handle(TRequest request, CancellationToken cancellationToken = default);
}

public interface IRequestHandler<in TRequest, TResult> : IBaseRequestHandler
    where TRequest : IRequest<TResult>
{
    ValueTask<TResult> Handle(TRequest request, CancellationToken cancellationToken = default);
}
