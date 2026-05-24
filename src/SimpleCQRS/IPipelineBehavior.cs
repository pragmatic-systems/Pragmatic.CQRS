namespace SimpleCQRS;

public interface IPipelineBehavior<in TInput, TOutput>
{
    Task<TOutput> Handle(TInput input, Func<Task<TOutput>> next, CancellationToken cancellationToken = default);
}

public interface IPipelineBehavior<in TInput>
{
    Task Handle(TInput input, Func<Task> next, CancellationToken cancellationToken = default);
}
