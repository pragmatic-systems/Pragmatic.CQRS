using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;
using Xunit.Sdk;

namespace Pragmatic.CQRS.Tests;

public class MediatorTests
{
    [Fact]
    public void RegisterServicesFromAssemblies_DiscoversTypedRequestHandler()
    {
        var services = new ServiceCollection();
        services.AddCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                new[] { typeof(MediatorTests).Assembly });
        });

        var descriptor = services.FirstOrDefault(s =>
            s.ServiceType == typeof(IRequestHandler<LoggingQuery, int>));
        descriptor.ShouldNotBeNull();
        descriptor!.ImplementationType.ShouldBe(typeof(LoggingQueryHandler));

        var descriptor2 = services.FirstOrDefault(s =>
            s.ServiceType == typeof(IRequestHandler<VoidLoggingCommand>));
        descriptor2.ShouldNotBeNull();
        descriptor2!.ImplementationType.ShouldBe(typeof(VoidLoggingCommandHandler));
    }

    [Fact]
    public async Task Send_WithResult_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var provider = BuildContainer();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mediator.Send(new LoggingQuery(1), cts.Token));
    }

    [Fact]
    public async Task Send_WithResult_CachesReflection()
    {
        var provider = BuildContainer();
        var mediator = provider.GetRequiredService<IMediator>();

        var t1 = await mediator.Send(new LoggingQuery(1), TestContext.Current.CancellationToken);
        var t2 = await mediator.Send(new LoggingQuery(2), TestContext.Current.CancellationToken);
        var handler = (LoggingQueryHandler)provider.GetRequiredService<IRequestHandler<LoggingQuery, int>>();

        Assert.Equal(2, t1);
        Assert.Equal(4, t2);

        Assert.Equal(2, handler.InvocationCount);
    }

    [Fact]
    public async Task Send_WithoutResult_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var provider = BuildContainer();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mediator.Send(new VoidLoggingCommand(), cts.Token));
    }

    [Fact]
    public async Task Send_WithMultipleBehaviors_ChainsInReverseOrder()
    {
        var logs = new List<string>();
        var behaviorA = new LoggingBehavior("A", logs);
        var behaviorB = new LoggingBehavior("B", logs);

        var provider = BuildContainer(behaviorA, behaviorB);
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new LoggingQuery(1), TestContext.Current.CancellationToken);
        var handler = (LoggingQueryHandler)provider.GetRequiredService<IRequestHandler<LoggingQuery, int>>();

        // Behaviors chain in reverse: B wraps A wraps handler
        Assert.Equal(
            [
            "B-before",
            "A-before",
            "A-after",
            "B-after"
        ], logs);

        handler.InvocationCount.ShouldBe(1);
    }

    [Fact]
    public async Task SendVoid_WithMultipleBehaviors_ChainsBehaviorAroundHandler()
    {
        var logs = new List<string>();
        var behaviorA = new VoidLoggingBehavior("A", logs);
        var behaviorB = new VoidLoggingBehavior("B", logs);

        var provider = BuildContainer(behaviorA, behaviorB);
        var mediator = provider.GetRequiredService<IMediator>();

        var handler = (VoidLoggingCommandHandler)provider.GetRequiredService<IRequestHandler<VoidLoggingCommand>>();
        await mediator.Send(new VoidLoggingCommand(), TestContext.Current.CancellationToken);

        Assert.Equal(1, handler.InvocationCount);

        // Behaviors chain in reverse: B wraps A wraps handler
        Assert.Equal(
            [
            "B-before",
            "A-before",
            "A-after",
            "B-after"
        ], logs);
    }

    [Fact]
    public async Task Send_WithMissingHandler_ThrowsCqrsException()
    {
        var provider = BuildContainer();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<CqrsException>(() =>
            mediator.Send(new UnknownQuery(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Send_OpenGenericPipelineBehavior_AppliesToMultipleRequestTypes()
    {
        // Register a single open-generic pipeline behavior that should wrap ALL request types.
        var logs = new List<string>();

        var services = new ServiceCollection();
        services.AddCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                new[] { typeof(MediatorTests).Assembly }, ServiceLifetime.Singleton);
        });

        // The shared log must be registered so DI can inject it into the open-generic behavior.
        services.AddSingleton(logs);

        // Register as open generic — one registration, applies to every IRequest<IResult>.
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(GenericPipelineBehavior<,>));

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Dispatch two completely different request types.
        var resultA = await mediator.Send(new OpenGenericQueryA(5), TestContext.Current.CancellationToken);
        var resultB = await mediator.Send(new OpenGenericQueryB("hello"), TestContext.Current.CancellationToken);

        // Verify handler results are correct.
        Assert.Equal(15, resultA);
        Assert.Equal("echo:hello", resultB);

        // Verify the open-generic behavior wrapped BOTH requests.
        Assert.Equal(
            [
                "GenericBehavior<OpenGenericQueryA,Int32>-before",
                "GenericBehavior<OpenGenericQueryA,Int32>-after",
                "GenericBehavior<OpenGenericQueryB,String>-before",
                "GenericBehavior<OpenGenericQueryB,String>-after",
            ], logs);

        // Verify each handler was called exactly once.
        var handlerA = (OpenGenericQueryAHandler)provider.GetRequiredService<IRequestHandler<OpenGenericQueryA, int>>();
        var handlerB = (OpenGenericQueryBHandler)provider.GetRequiredService<IRequestHandler<OpenGenericQueryB, string>>();
        handlerA.InvocationCount.ShouldBe(1);
        handlerB.InvocationCount.ShouldBe(1);
    }

    private IServiceProvider BuildContainer(params IPipelineBehavior[] pipelines)
    {
        var services = new ServiceCollection();
        services.InitializeTestServices(pipelines);
        return services.BuildServiceProvider();
    }
}
