using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Pragmatic.CQRS.Tests;

public class ExceptionUnwrappingTests
{
    private sealed record ThrowingQuery : IRequest<int>;

    private sealed class ThrowingQueryHandler : IRequestHandler<ThrowingQuery, int>
    {
        public Task<int> Handle(ThrowingQuery request, CancellationToken cancellationToken = default)
            => throw new TestBusinessException("boom");
    }

    private sealed record VoidThrowingCommand : IRequest;

    private sealed class VoidThrowingCommandHandler : IRequestHandler<VoidThrowingCommand>
    {
        public Task Handle(VoidThrowingCommand request, CancellationToken cancellationToken = default)
            => throw new TestBusinessException("void boom");
    }

    private sealed record InnerChainQuery : IRequest<string>;

    private sealed class InnerChainQueryHandler : IRequestHandler<InnerChainQuery, string>
    {
        public Task<string> Handle(InnerChainQuery request, CancellationToken cancellationToken = default)
        {
            try
            {
                throw new DivideByZeroException("divide");
            }
            catch (DivideByZeroException ex)
            {
                throw new TestBusinessException("wrapped", ex);
            }
        }
    }

    private sealed record BehaviorTestQuery : IRequest<int>;

    private sealed class BehaviorTestHandler : IRequestHandler<BehaviorTestQuery, int>
    {
        public int InvocationCount { get; private set; }

        public Task<int> Handle(BehaviorTestQuery request, CancellationToken cancellationToken = default)
        {
            InvocationCount++;
            return Task.FromResult(42);
        }
    }

    private sealed class ThrowingBeforeBehavior : IPipelineBehavior<BehaviorTestQuery, int>
    {
        public List<string> Log { get; } = new();

        public Task<int> Handle(BehaviorTestQuery input, RequestHandlerDelegate<int> next, CancellationToken cancellationToken = default)
        {
            Log.Add("before");
            throw new TestBusinessException("behavior boom");
        }
    }

    private sealed class OuterCatchBehavior : IPipelineBehavior<BehaviorTestQuery, int>
    {
        public List<string> Log { get; } = new();

        public async Task<int> Handle(BehaviorTestQuery input, RequestHandlerDelegate<int> next, CancellationToken cancellationToken = default)
        {
            Log.Add("outer-before");
            try
            {
                var result = await next();
                Log.Add("outer-after");
                return result;
            }
            catch
            {
                Log.Add("outer-caught");
                throw;
            }
        }
    }

    private sealed class ThrowingBehaviorHandler : IRequestHandler<BehaviorTestQuery, int>
    {
        public Task<int> Handle(BehaviorTestQuery request, CancellationToken cancellationToken = default)
            => throw new TestBusinessException("handler boom");
    }

    private sealed class TestBusinessException : Exception
    {
        public TestBusinessException(string message)
            : base(message) { }

        public TestBusinessException(string message, Exception inner)
            : base(message, inner) { }
    }

    [Fact]
    public async Task Send_WithResult_HandlerThrows_OriginalExceptionUnwrapped()
    {
        var mediator = BuildMediator<IRequestHandler<ThrowingQuery, int>, ThrowingQueryHandler>();

        var exception = await mediator.Send(new ThrowingQuery(), TestContext.Current.CancellationToken)
            .ShouldThrowAsync<TestBusinessException>();

        exception.Message.ShouldBe("boom");
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public async Task Send_Void_HandlerThrows_OriginalExceptionUnwrapped()
    {
        var mediator = BuildMediator<IRequestHandler<VoidThrowingCommand>, VoidThrowingCommandHandler>();

        var exception = await mediator.Send(new VoidThrowingCommand(), TestContext.Current.CancellationToken)
            .ShouldThrowAsync<TestBusinessException>();

        exception.Message.ShouldBe("void boom");
        exception.InnerException.ShouldBeNull();
    }

    [Fact]
    public async Task Send_WithResult_BehaviorThrowsBeforeHandler_HandlerNotInvoked()
    {
        var behavior = new ThrowingBeforeBehavior();
        var mediator = BuildMediatorWithBehavior(
            behavior,
            (IServiceCollection sc) => sc.AddSingleton<IRequestHandler<BehaviorTestQuery, int>, BehaviorTestHandler>());

        var exception = await mediator.Send(new BehaviorTestQuery(), TestContext.Current.CancellationToken)
            .ShouldThrowAsync<TestBusinessException>();

        exception.Message.ShouldBe("behavior boom");
        behavior.Log.ShouldBe(["before"]);
    }

    [Fact]
    public async Task Send_WithResult_HandlerThrows_OuterBehaviorCanObserve()
    {
        var outerBehavior = new OuterCatchBehavior();
        var mediator = BuildMediatorWithBehavior(
            outerBehavior,
            (IServiceCollection sc) => sc.AddSingleton<IRequestHandler<BehaviorTestQuery, int>, ThrowingBehaviorHandler>());

        await mediator.Send(new BehaviorTestQuery(), TestContext.Current.CancellationToken)
            .ShouldThrowAsync<TestBusinessException>();

        outerBehavior.Log.ShouldBe(["outer-before", "outer-caught"]);
    }

    [Fact]
    public async Task Send_WithResult_InnerExceptionChain_Preserved()
    {
        var mediator = BuildMediator<IRequestHandler<InnerChainQuery, string>, InnerChainQueryHandler>();

        var exception = await mediator.Send(new InnerChainQuery(), TestContext.Current.CancellationToken)
            .ShouldThrowAsync<TestBusinessException>();

        exception.InnerException.ShouldBeOfType<DivideByZeroException>();
        exception.InnerException!.Message.ShouldBe("divide");
    }

    private static IMediator BuildMediator<TService, TImplementation>()
        where TService : class
        where TImplementation : class, TService
    {
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddSingleton<MediatorCacheMap>();
        services.AddSingleton<TService, TImplementation>();
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }

    private static IMediator BuildMediatorWithBehavior(
        IPipelineBehavior<BehaviorTestQuery, int> behavior,
        Action<IServiceCollection> configureHandler)
    {
        var services = new ServiceCollection();
        services.AddTransient<IMediator, Mediator>();
        services.AddSingleton<MediatorCacheMap>();
        services.AddSingleton(behavior);
        configureHandler(services);
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }
}
