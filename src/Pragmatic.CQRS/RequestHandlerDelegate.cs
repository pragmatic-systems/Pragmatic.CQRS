namespace Pragmatic.CQRS;

public delegate Task RequestHandlerDelegate(CancellationToken cancellationToken);

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken);
