using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Monbsoft.BrilliantMediator.Abstractions;

/// <summary>
/// Registry for handler type registration.
/// Used at startup/configuration time to register handler types for DI resolution.
/// Application code should depend on <see cref="IMediator"/> for dispatching, not this interface.
/// </summary>
public interface IHandlerRegistry
{
    /// <summary>
    /// Registers a command handler type for DI resolution.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    void RegisterCommandHandler<TCommand>()
        where TCommand : ICommand;

    /// <summary>
    /// Registers a command handler type with response for DI resolution.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    void RegisterCommandHandler<TCommand, TResponse>()
        where TCommand : ICommand<TResponse>;

    /// <summary>
    /// Registers a query handler type for DI resolution.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    void RegisterQueryHandler<TQuery, TResponse>()
        where TQuery : IQuery<TResponse>;

    /// <summary>
    /// Registers an event handler type for DI resolution.
    /// Multiple handlers can be registered for the same event.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    void RegisterEventHandler<TEvent>()
        where TEvent : IEvent;
}
