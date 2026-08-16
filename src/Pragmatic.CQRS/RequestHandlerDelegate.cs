namespace Pragmatic.CQRS;

public delegate Task RequestHandlerDelegate(CancellationToken cancellationToken = default);

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>(CancellationToken cancellationToken = default);
