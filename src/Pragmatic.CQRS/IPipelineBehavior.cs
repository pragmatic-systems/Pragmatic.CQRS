namespace Pragmatic.CQRS;

public interface IPipelineBehavior { }

public interface IPipelineBehavior<in TInput, TOutput> : IPipelineBehavior
{
    ValueTask<TOutput> Handle(TInput input, RequestHandlerDelegate<TOutput> next, CancellationToken cancellationToken = default);
}

public interface IPipelineBehavior<in TInput> : IPipelineBehavior
{
    ValueTask Handle(TInput input, RequestHandlerDelegate next, CancellationToken cancellationToken = default);
}
