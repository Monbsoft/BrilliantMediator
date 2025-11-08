using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Exceptions;

namespace Monbsoft.BrilliantMediator.Core;

/// <summary>
/// Ultra-lightweight, zero-reflection mediator implementation.
/// Uses compiled generics for maximum performance.
/// </summary>
public sealed class Mediator : IMediator
{
    /// <summary>
    /// Registry for command handlers without response.
    /// Each TCommand gets its own static reference - zero reflection, O(1) lookup.
    /// </summary>
    private sealed class CommandHandlerRegistry<TCommand> where TCommand : ICommand
    {
        public static ICommandHandler<TCommand>? Instance { get; set; }
    }

    /// <summary>
    /// Registry for command handlers with response.
    /// Each TCommand gets its own static reference - zero reflection, O(1) lookup.
    /// </summary>
    private sealed class CommandHandlerRegistry<TCommand, TResponse> where TCommand : ICommand<TResponse>
    {
        public static ICommandHandler<TCommand, TResponse>? Instance { get; set; }
    }

    /// <summary>
    /// Registry for query handlers.
    /// Each TQuery gets its own static reference - zero reflection, O(1) lookup.
    /// </summary>
    private sealed class QueryHandlerRegistry<TQuery, TResponse> where TQuery : IQuery<TResponse>
    {
        public static IQueryHandler<TQuery, TResponse>? Instance { get; set; }
    }

    /// <summary>
    /// Registers a handler for a command without response.
    /// O(1), no allocations, no reflection.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="handler">The handler instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if handler is null.</exception>
    public void RegisterCommandHandler<TCommand>(ICommandHandler<TCommand> handler)
        where TCommand : ICommand
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        CommandHandlerRegistry<TCommand>.Instance = handler;
    }

    /// <summary>
    /// Registers a handler for a command with response.
    /// O(1), no allocations, no reflection.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The handler instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if handler is null.</exception>
    public void RegisterCommandHandler<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> handler)
        where TCommand : ICommand<TResponse>
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        CommandHandlerRegistry<TCommand, TResponse>.Instance = handler;
    }

    /// <summary>
    /// Registers a handler for a query.
    /// O(1), no allocations, no reflection.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The handler instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if handler is null.</exception>
    public void RegisterQueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> handler)
        where TQuery : IQuery<TResponse>
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        QueryHandlerRegistry<TQuery, TResponse>.Instance = handler;
    }

    /// <summary>
    /// Sends a command without response.
    /// O(1), inlinable by JIT, near-zero overhead.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <returns>A task that completes when the command is handled.</returns>
    /// <exception cref="HandlerNotRegisteredException">Thrown if no handler is registered.</exception>
    public async Task DispatchAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        var handler = CommandHandlerRegistry<TCommand>.Instance;
        if (handler == null)
            throw HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name);

        await handler.Handle(command).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a command with response.
    /// O(1), inlinable by JIT, near-zero overhead.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <returns>A task that completes with the response when the command is handled.</returns>
    /// <exception cref="HandlerNotRegisteredException">Thrown if no handler is registered.</exception>
    public async Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command)
        where TCommand : ICommand<TResponse>
    {
        var handler = CommandHandlerRegistry<TCommand, TResponse>.Instance;
        if (handler == null)
            throw HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name);

        return await handler.Handle(command).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a query.

    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="query">The query to send.</param>
    /// <returns>A task that completes with the response when the query is handled.</returns>
    /// <exception cref="HandlerNotRegisteredException">Thrown if no handler is registered.</exception>
    public async Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query)
        where TQuery : IQuery<TResponse>
    {
        var handler = QueryHandlerRegistry<TQuery, TResponse>.Instance;
        if (handler == null)
            throw HandlerNotRegisteredException.ForQuery(typeof(TQuery).Name);

        return await handler.Handle(query).ConfigureAwait(false);
    }
}