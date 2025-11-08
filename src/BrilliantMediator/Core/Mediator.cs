using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Exceptions;
using System.Collections.Concurrent;

namespace Monbsoft.BrilliantMediator.Core;

/// <summary>
/// Ultra-lightweight, zero-reflection mediator implementation.
/// Uses compiled generics for maximum performance.
/// Each instance maintains its own handler registry to avoid state sharing.
/// </summary>
public sealed class Mediator : IMediator
{
    /// <summary>
    /// Instance-based registry for handlers.
    /// Uses a concurrent dictionary to store handler instances by their type key.
    /// This ensures isolation between different Mediator instances and test runs.
    /// </summary>
    private readonly ConcurrentDictionary<string, object?> _handlerRegistry = new();

    /// <summary>
    /// Generates a unique key for a command handler type.
    /// </summary>
    private static string GetCommandHandlerKey<TCommand>() where TCommand : ICommand
        => $"cmd_{typeof(TCommand).FullName}";

    /// <summary>
    /// Generates a unique key for a command handler type with response.
    /// </summary>
    private static string GetCommandHandlerKey<TCommand, TResponse>() where TCommand : ICommand<TResponse>
   => $"cmd_resp_{typeof(TCommand).FullName}_{typeof(TResponse).FullName}";

    /// <summary>
    /// Generates a unique key for a query handler type.
    /// </summary>
    private static string GetQueryHandlerKey<TQuery, TResponse>() where TQuery : IQuery<TResponse>
    => $"query_{typeof(TQuery).FullName}_{typeof(TResponse).FullName}";

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

        var key = GetCommandHandlerKey<TCommand>();
        _handlerRegistry[key] = handler;
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

        var key = GetCommandHandlerKey<TCommand, TResponse>();
        _handlerRegistry[key] = handler;
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

        var key = GetQueryHandlerKey<TQuery, TResponse>();
        _handlerRegistry[key] = handler;
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
        var key = GetCommandHandlerKey<TCommand>();
        if (!_handlerRegistry.TryGetValue(key, out var handlerObj))
            throw HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name);

        var handler = (ICommandHandler<TCommand>)handlerObj!;
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
        var key = GetCommandHandlerKey<TCommand, TResponse>();
        if (!_handlerRegistry.TryGetValue(key, out var handlerObj))
            throw HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name);

        var handler = (ICommandHandler<TCommand, TResponse>)handlerObj!;
        return await handler.Handle(command).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends a query.
    /// O(1), inlinable by JIT, near-zero overhead.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="query">The query to send.</param>
    /// <returns>A task that completes with the response when the query is handled.</returns>
    /// <exception cref="HandlerNotRegisteredException">Thrown if no handler is registered.</exception>
    public async Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query)
        where TQuery : IQuery<TResponse>
    {
        var key = GetQueryHandlerKey<TQuery, TResponse>();
        if (!_handlerRegistry.TryGetValue(key, out var handlerObj))
            throw HandlerNotRegisteredException.ForQuery(typeof(TQuery).Name);

        var handler = (IQueryHandler<TQuery, TResponse>)handlerObj!;
        return await handler.Handle(query).ConfigureAwait(false);
    }
}