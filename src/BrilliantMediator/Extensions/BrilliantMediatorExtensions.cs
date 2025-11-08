using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Core;

namespace Monbsoft.BrilliantMediator.Extensions;

/// <summary>
/// Extension methods for adding BrilliantMediator to the DI container.
/// ZERO REFLECTION - Requires manual handler registration.
/// </summary>
public static class BrilliantMediatorExtensions
{
    /// <summary>
    /// Adds BrilliantMediator to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A builder for fluent handler registration.</returns>
    public static MediatorBuilder AddBrilliantMediator(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register Mediator as singleton
        services.AddSingleton<IMediator, Mediator>();

        // Return builder for fluent registration
        return new MediatorBuilder(services);
    }
}