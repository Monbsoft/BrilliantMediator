using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Monbsoft.BrilliantMediator.Abstractions;

/// <summary>
/// Core mediator interface for dispatching commands, queries, and events.
/// Zero-reflection, compile-time type-safe.
/// </summary>
public interface IMediator
{
    /// <summary>
    /// Dispatches a command without response.
    /// </summary>
    Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;

    /// <summary>
    /// Dispatches a command with response.
    /// </summary>
    Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>;

    /// <summary>
    /// Sends a query.
    /// </summary>
    Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>;

    /// <summary>
    /// Publishes an event to all registered handlers.
    /// Handlers are executed in parallel.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent;
}
