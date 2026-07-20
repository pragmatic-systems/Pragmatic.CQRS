using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace Pragmatic.CQRS.Tests;

public class NotificationTests
{
    [Fact]
    public async Task Publish_AllHandlers_InvokedConcurrently()
    {
        var provider = BuildNotificationContainer();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new DomainEventOccurred("TestEvent"), TestContext.Current.CancellationToken);

        var handlers = provider.GetServices<INotificationHandler<DomainEventOccurred>>();
        var first = (DomainEventFirstHandler)handlers.First();
        var second = (DomainEventSecondHandler)handlers.Last();

        first.InvocationCount.ShouldBe(1);
        first.ReceivedEventName.ShouldBe("TestEvent");

        second.InvocationCount.ShouldBe(1);
        second.ReceivedEventName.ShouldBe("TestEvent");
    }

    [Fact]
    public async Task Publish_AllHandlers_ExecuteOnIndividualAsyncError()
    {
        var provider = BuildNotificationContainer(services =>
        {
            services.AddSingleton<INotificationHandler<DomainEventOccurred>, AsyncErrorHandler>();
        });

        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new DomainEventOccurred("TestEvent"), TestContext.Current.CancellationToken);

        var handlers = provider.GetServices<INotificationHandler<DomainEventOccurred>>();
        var first = handlers.OfType<DomainEventFirstHandler>().First();
        var second = handlers.OfType<DomainEventSecondHandler>().First();

        first.InvocationCount.ShouldBe(1);
        first.ReceivedEventName.ShouldBe("TestEvent");

        second.InvocationCount.ShouldBe(1);
        second.ReceivedEventName.ShouldBe("TestEvent");
    }

    [Fact]
    public async Task Publish_AllHandlers_ExecuteOnIndividualSyncError()
    {
        var provider = BuildNotificationContainer(services =>
        {
            services.AddSingleton<INotificationHandler<DomainEventOccurred>, SyncErrorHandler>();
        });

        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Publish(new DomainEventOccurred("TestEvent"), TestContext.Current.CancellationToken);

        var handlers = provider.GetServices<INotificationHandler<DomainEventOccurred>>();
        var first = handlers.OfType<DomainEventFirstHandler>().First();
        var second = handlers.OfType<DomainEventSecondHandler>().First();

        first.InvocationCount.ShouldBe(1);
        first.ReceivedEventName.ShouldBe("TestEvent");

        second.InvocationCount.ShouldBe(1);
        second.ReceivedEventName.ShouldBe("TestEvent");
    }

    [Fact]
    public async Task Publish_PassesCancellationToken()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var provider = BuildNotificationContainer();
        var mediator = provider.GetRequiredService<IMediator>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            mediator.Publish(new CancellationNotification(), cts.Token));
    }

    [Fact]
    public async Task Publish_NoHandlers_CompletesWithoutError()
    {
        var services = new ServiceCollection();
        services.AddCqrs();
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        // Should not throw even with no handlers registered
        await mediator.Publish(new DomainEventOccurred("TestEvent"), TestContext.Current.CancellationToken);
    }

    private IServiceProvider BuildNotificationContainer(Action<IServiceCollection> enrich = null)
    {
        var services = new ServiceCollection();

        services.AddTransient<IMediator, Mediator>();
        services.AddSingleton<MediatorCacheMap>();
        services.AddSingleton<INotificationHandler<DomainEventOccurred>, DomainEventFirstHandler>();
        services.AddSingleton<INotificationHandler<DomainEventOccurred>, DomainEventSecondHandler>();
        services.AddSingleton<INotificationHandler<CancellationNotification>, CancellationNotificationHandler>();

        if (enrich != null)
            enrich(services);

        return services.BuildServiceProvider();
    }
}
