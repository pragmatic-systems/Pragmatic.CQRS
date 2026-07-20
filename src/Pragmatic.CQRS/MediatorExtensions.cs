using Microsoft.Extensions.DependencyInjection;

namespace Pragmatic.CQRS;

public static class MediatorExtensions
{
    public static IServiceCollection AddCqrs(this IServiceCollection services, Action<MediatorConfig>? config = null)
    {
        services.AddTransient<IMediator, Mediator>();
        services.AddSingleton<MediatorCacheMap>();

        var configurationBuilder = new MediatorConfig(services);
        if (config != null)
            config(configurationBuilder);

        return services;
    }
}
