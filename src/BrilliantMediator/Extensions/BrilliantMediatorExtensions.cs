using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Core;

namespace Monbsoft.BrilliantMediator.Extensions;

public static class BrilliantMediatorExtensions
{
    /// <summary>
    /// Adds BrilliantMediator to the service collection.
    /// Use the returned builder for handler registration, then call Build().
    /// Must call app.UseBrilliantMediator() to initialize handlers.
    /// </summary>
    public static MediatorBuilder AddBrilliantMediator(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddSingleton<IMediator>(provider => new Mediator(provider));

        return new MediatorBuilder(services);
    }

    /// <summary>
    /// Initializes BrilliantMediator by registering all handlers with the mediator instance.
    /// Must be called after building the application and before using the mediator.
    /// </summary>
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