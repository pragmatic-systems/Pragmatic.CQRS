namespace Pragmatic.CQRS;

public interface IPipelineBehavior { }

public interface IPipelineBehavior<in TInput, TOutput> : IPipelineBehavior
{
    Task<TOutput> Handle(TInput input, RequestHandlerDelegate<TOutput> next, CancellationToken cancellationToken = default);
}

public struct Unit
{
    public static readonly Unit Instance = default;
}
