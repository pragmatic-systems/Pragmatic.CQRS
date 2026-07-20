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

    private IServiceProvider BuildNotificationContainer()
    {
        var services = new ServiceCollection();
        services.AddCqrs(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
                new[] { typeof(MediatorTests).Assembly }, ServiceLifetime.Singleton);
        });

        return services.BuildServiceProvider();
    }
}
