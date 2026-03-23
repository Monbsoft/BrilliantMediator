using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Monbsoft.BrilliantMediator.Extensions;

/// <summary>
/// Builder for fluent registration of handlers.
/// Zero reflection — all at compile-time with generics.
/// </summary>
public sealed class MediatorBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Action<IHandlerRegistry>> _handlerRegistrations = new();

    public MediatorBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers a command handler without response.
    /// </summary>
    public MediatorBuilder AddCommandHandler<TCommand, THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        _services.Add(new ServiceDescriptor(typeof(ICommandHandler<TCommand>), typeof(THandler), lifetime));
        _handlerRegistrations.Add(registry => registry.RegisterCommandHandler<TCommand>());
        return this;
    }

    /// <summary>
    /// Registers a command handler with response.
    /// </summary>
    public MediatorBuilder AddCommandHandler<TCommand, TResponse, THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TCommand : ICommand<TResponse>
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        _services.Add(new ServiceDescriptor(typeof(ICommandHandler<TCommand, TResponse>), typeof(THandler), lifetime));
        _handlerRegistrations.Add(registry => registry.RegisterCommandHandler<TCommand, TResponse>());
        return this;
    }

    /// <summary>
    /// Registers a query handler.
    /// </summary>
    public MediatorBuilder AddQueryHandler<TQuery, TResponse, THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TQuery : IQuery<TResponse>
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        _services.Add(new ServiceDescriptor(typeof(IQueryHandler<TQuery, TResponse>), typeof(THandler), lifetime));
        _handlerRegistrations.Add(registry => registry.RegisterQueryHandler<TQuery, TResponse>());
        return this;
    }

    /// <summary>
    /// Registers an event handler.
    /// Multiple handlers can be registered for the same event.
    /// </summary>
    public MediatorBuilder AddEventHandler<TEvent, THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TEvent : IEvent
        where THandler : class, IEventHandler<TEvent>
    {
        _services.Add(new ServiceDescriptor(typeof(IEventHandler<TEvent>), typeof(THandler), lifetime));
        _handlerRegistrations.Add(registry => registry.RegisterEventHandler<TEvent>());
        return this;
    }

    /// <summary>
    /// Finalizes the builder and returns the service collection.
    /// </summary>
    public IServiceCollection Build()
    {
        var registrations = _handlerRegistrations.ToArray();
        _services.AddSingleton<IMediatorInitializer>(new MediatorInitializer(registrations));
        return _services;
    }
}

internal sealed class MediatorInitializer : IMediatorInitializer
{
    private readonly Action<IHandlerRegistry>[] _registrations;

    public MediatorInitializer(Action<IHandlerRegistry>[] registrations)
    {
        _registrations = registrations;
    }

    public void Initialize(IHandlerRegistry registry)
    {
        foreach (var registration in _registrations)
        {
            registration(registry);
        }
    }
}
