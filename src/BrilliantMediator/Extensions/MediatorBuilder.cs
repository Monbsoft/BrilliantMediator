using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Pipeline;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using System.Diagnostics.CodeAnalysis;

namespace Monbsoft.BrilliantMediator.Extensions;

/// <summary>
/// Builder for fluent registration of handlers.
/// Zero reflection — all at compile-time with generics.
/// </summary>
public sealed class MediatorBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Action<IHandlerRegistry>> _handlerRegistrations = new();

    /// <summary>
    /// Creates a builder registering handlers and behaviors into the given collection.
    /// </summary>
    /// <param name="services">The service collection to populate.</param>
    public MediatorBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers a command handler without response.
    /// </summary>
    public MediatorBuilder AddCommandHandler<TCommand, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
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
    public MediatorBuilder AddCommandHandler<TCommand, TResponse, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
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
    public MediatorBuilder AddQueryHandler<TQuery, TResponse, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
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
    public MediatorBuilder AddEventHandler<TEvent, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TEvent : IEvent
        where THandler : class, IEventHandler<TEvent>
    {
        _services.Add(new ServiceDescriptor(typeof(IEventHandler<TEvent>), typeof(THandler), lifetime));
        _handlerRegistrations.Add(registry => registry.RegisterEventHandler<TEvent>());
        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior around a query or a command with response.
    /// Behaviors run in registration order, the first registered being the
    /// outermost one (ADR-008).
    /// </summary>
    /// <typeparam name="TRequest">The request type the behavior applies to.</typeparam>
    /// <typeparam name="TResponse">The response type of that request.</typeparam>
    /// <typeparam name="TBehavior">The behavior implementation.</typeparam>
    /// <param name="lifetime">The DI lifetime of the behavior. Scoped by default.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <remarks>
    /// <typeparamref name="TBehavior"/> may be a closed generic such as
    /// <c>LoggingBehavior&lt;GetUserQuery, UserDto&gt;</c>: the closure is built
    /// by the compiler, so resolution stays reflection-free (ADR-010).
    /// <para>
    /// <typeparamref name="TRequest"/> and <typeparamref name="TResponse"/> are not
    /// constrained — the same pair serves queries and commands with response — so a
    /// pair that matches no dispatched request still registers, and the behavior
    /// silently never runs. Keep both type arguments identical to the handler
    /// registration.
    /// </para>
    /// </remarks>
    public MediatorBuilder AddPipelineBehavior<TRequest, TResponse, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TBehavior>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
    {
        _services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<TRequest, TResponse>), typeof(TBehavior), lifetime));
        _handlerRegistrations.Add(registry => registry.RegisterPipelineBehavior<TRequest, TResponse>());
        return this;
    }

    /// <summary>
    /// Registers a pipeline behavior around a command without response.
    /// Behaviors run in registration order, the first registered being the
    /// outermost one (ADR-008).
    /// </summary>
    /// <typeparam name="TRequest">The command type the behavior applies to.</typeparam>
    /// <typeparam name="TBehavior">The behavior implementation.</typeparam>
    /// <param name="lifetime">The DI lifetime of the behavior. Scoped by default.</param>
    /// <returns>The same builder, for chaining.</returns>
    public MediatorBuilder AddPipelineBehavior<TRequest, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TBehavior>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TRequest : ICommand
        where TBehavior : class, IPipelineBehavior<TRequest>
    {
        _services.Add(new ServiceDescriptor(typeof(IPipelineBehavior<TRequest>), typeof(TBehavior), lifetime));
        _handlerRegistrations.Add(registry => registry.RegisterPipelineBehavior<TRequest>());
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
