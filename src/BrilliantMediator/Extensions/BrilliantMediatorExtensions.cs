using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Core;
using System.Reflection;

namespace Monbsoft.BrilliantMediator.Extensions;

/// <summary>
/// Extension methods for adding BrilliantMediator to the DI container.
/// Supports manual registration and automatic discovery from assemblies.
/// </summary>
public static class BrilliantMediatorExtensions
{
    /// <summary>
    /// Adds BrilliantMediator to the service collection with no automatic discovery.
    /// Use the returned builder for manual handler registration.
    /// Must call app.UseBrilliantMediator() to initialize handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A builder for fluent handler registration.</returns>
    public static MediatorBuilder AddBrilliantMediator(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register Mediator as singleton WITH IServiceProvider support
        services.AddSingleton<IMediator>(provider =>
        {
            var mediator = new Mediator(provider);
            return mediator;
        });

        // Return builder for fluent registration
        return new MediatorBuilder(services);
    }

    /// <summary>
    /// Adds BrilliantMediator with automatic discovery of handlers from a single assembly.
    /// Discovers and registers ICommandHandler, IQueryHandler, and IEventHandler implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <param name="lifetime">The service lifetime for discovered handlers.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBrilliantMediator(
        this IServiceCollection services,
        Assembly assembly,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var builder = services.AddBrilliantMediator();
        builder.AddHandlersFromAssembly(assembly, lifetime).Build();

        return services;
    }

    /// <summary>
    /// Adds BrilliantMediator with automatic discovery of handlers from multiple assemblies.
    /// Discovers and registers ICommandHandler, IQueryHandler, and IEventHandler implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <param name="lifetime">The service lifetime for discovered handlers.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBrilliantMediator(
        this IServiceCollection services,
        IEnumerable<Assembly> assemblies,
        ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (assemblies == null)
            throw new ArgumentNullException(nameof(assemblies));

        var builder = services.AddBrilliantMediator();
        builder.AddHandlersFromAssemblies(assemblies, lifetime).Build();

        return services;
    }

    /// <summary>
    /// Adds BrilliantMediator with automatic discovery of handlers from multiple assemblies (params overload).
    /// Discovers and registers ICommandHandler, IQueryHandler, and IEventHandler implementations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddBrilliantMediator(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (assemblies == null)
            throw new ArgumentNullException(nameof(assemblies));

        var builder = services.AddBrilliantMediator();
        builder.AddHandlersFromAssemblies(assemblies).Build();

        return services;
    }

    /// <summary>
    /// Initializes BrilliantMediator by registering all handlers with the mediator instance.
    /// Must be called after building the application and before using the mediator.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The application builder for method chaining.</returns>
    public static IApplicationBuilder UseBrilliantMediator(this IApplicationBuilder app)
    {
        if (app == null)
            throw new ArgumentNullException(nameof(app));

        var mediator = app.ApplicationServices.GetRequiredService<IMediator>();
        var initializer = app.ApplicationServices.GetRequiredService<IMediatorInitializer>();
        initializer.Initialize(mediator);
        
        return app;
    }

}