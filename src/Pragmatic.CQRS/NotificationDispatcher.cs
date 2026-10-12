using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Pragmatic.CQRS;

public class NotificationDispatcher<TNotification> : NotificationDispatcherBase
    where TNotification : INotification
{
    private readonly ILogger<Mediator>? _logger;

    public NotificationDispatcher(ILogger<Mediator>? logger = null)
    {
        _logger = logger;
    }

    public override async Task Invoke(IServiceProvider provider, INotification notification, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(notification);

        var notificationType = notification.GetType();

        // Transient lifespan here - can't cache and re-use.
        var handlers = provider.GetServices<INotificationHandler<TNotification>>().ToArray();

        if (handlers.Length == 0)
        {
            _logger?.LogDebug("No handlers registered for notification type '{NotificationType}'. Notification will be silently dropped.", notificationType.FullName);
            return;
        }

        var typedNotification = (TNotification)notification;

        foreach (var handler in handlers)
        {
            if (handler == null) return;

            try
            {
                await handler.Handle(typedNotification, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;  // preserve cancellation semantics
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception occurred while notifying handler '{Handler}' for notification '{Notification}'", typeof(INotificationHandler<TNotification>).FullName, notificationType.FullName);
            }
        }
    }
}
