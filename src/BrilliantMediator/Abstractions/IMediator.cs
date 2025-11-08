using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Monbsoft.BrilliantMediator.Abstractions;

/// <summary>
/// Core mediator interface for command, query, and event handling.
/// Provides methods for:
/// - Dispatching commands (with or without response)
/// - Sending queries
/// - Publishing events to multiple handlers
/// Zero-reflection, compile-time type-safe.
/// </summary>
public interface IMediator
{
    #region Command Handlers

    /// <summary>
    /// Registers a command handler type for DI resolution.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    void RegisterCommandHandler<TCommand>()
        where TCommand : ICommand;

    /// <summary>
    /// Registers a command handler instance (for manual registration or testing).
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="handler">The handler instance.</param>
    void RegisterCommandHandler<TCommand>(ICommandHandler<TCommand> handler)
  where TCommand : ICommand;

    /// <summary>
    /// Registers a command handler type with response for DI resolution.
    /// </summary>
  /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
 void RegisterCommandHandler<TCommand, TResponse>()
 where TCommand : ICommand<TResponse>;

    /// <summary>
    /// Registers a command handler instance with response (for manual registration or testing).
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The handler instance.</param>
    void RegisterCommandHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> handler)
 where TCommand : ICommand<TResponse>;

    #endregion

    #region Query Handlers

    /// <summary>
    /// Registers a query handler type for DI resolution.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    void RegisterQueryHandler<TQuery, TResponse>()
        where TQuery : IQuery<TResponse>;

    /// <summary>
    /// Registers a query handler instance (for manual registration or testing).
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
 /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The handler instance.</param>
    void RegisterQueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> handler)
      where TQuery : IQuery<TResponse>;

 #endregion

    #region Event Handlers

    /// <summary>
    /// Registers an event handler type for DI resolution.
    /// Multiple handlers can be registered for the same event.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
  void RegisterEventHandler<TEvent>()
where TEvent : IEvent;

    /// <summary>
    /// Registers an event handler instance (for manual registration or testing).
/// Multiple handlers can be registered for the same event.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="handler">The handler instance.</param>
    void RegisterEventHandler<TEvent>(IEventHandler<TEvent> handler)
     where TEvent : IEvent;

    #endregion

    #region Command Dispatch

    /// <summary>
    /// Dispatches a command without response.
    /// </summary>
    Task DispatchAsync<TCommand>(TCommand command)
        where TCommand : ICommand;

    /// <summary>
    /// Dispatches a command with response.
    /// </summary>
    Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command)
        where TCommand : ICommand<TResponse>;

    #endregion

    #region Query Send

    /// <summary>
    /// Sends a query.
 /// </summary>
    Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query)
        where TQuery : IQuery<TResponse>;

    #endregion

    #region Event Publishing

    /// <summary>
    /// Publishes an event to all registered handlers.
    /// Handlers are executed in parallel (fire-and-forget).
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event)
      where TEvent : IEvent;

    #endregion
}