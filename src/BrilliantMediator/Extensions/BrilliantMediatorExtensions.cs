using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Core;

namespace Monbsoft.BrilliantMediator.Extensions;

public static class BrilliantMediatorExtensions
{
    /// <summary>
    /// Adds BrilliantMediator to the service collection.
    /// Use the returned builder for handler registration, then call Build().
    /// Call <see cref="UseBrilliantMediator"/> on the service provider to initialize handlers.
    /// </summary>
    public static MediatorBuilder AddBrilliantMediator(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddSingleton<Mediator>(provider => new Mediator(provider));
        services.AddSingleton<IMediator>(provider => provider.GetRequiredService<Mediator>());
        services.AddSingleton<IHandlerRegistry>(provider => provider.GetRequiredService<Mediator>());

        return new MediatorBuilder(services);
    }

    /// <summary>
    /// Initializes BrilliantMediator by registering all handlers with the handler registry.
    /// Works with any host (ASP.NET Core, Worker Service, Console app, etc.).
    /// </summary>
    public static IServiceProvider UseBrilliantMediator(this IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
            throw new ArgumentNullException(nameof(serviceProvider));

        var registry = serviceProvider.GetRequiredService<IHandlerRegistry>();
        var initializer = serviceProvider.GetRequiredService<IMediatorInitializer>();
        initializer.Initialize(registry);

        return serviceProvider;
    }
}
