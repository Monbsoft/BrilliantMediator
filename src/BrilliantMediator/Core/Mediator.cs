using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Exceptions;
using System.Collections.Concurrent;

namespace Monbsoft.BrilliantMediator.Core;

/// <summary>
/// Ultra-lightweight mediator implementation with DI support.
/// Uses IServiceProvider to resolve handlers on-demand, supporting proper scoping.
/// Each instance maintains its own handler type registry to track registrations.
/// Supports Commands, Queries, and Events with fire-and-forget event handling.
/// Handlers are resolved per request, allowing for scoped dependencies like DbContext.
/// </summary>
public sealed class Mediator : IMediator
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Registry to track which handler interface types are registered.
    /// Stores handler interface types by their message key for fast lookup.
    /// Used for DI-based resolution.
    /// </summary>
    private readonly ConcurrentDictionary<string, Type> _handlerTypeRegistry = new();

    /// <summary>
    /// Registry for event handler interface types.
    /// Multiple handler interface types can be registered for the same event type.
    /// Used for DI-based resolution.
    /// </summary>
    private readonly ConcurrentDictionary<string, List<Type>> _eventHandlerTypeRegistry = new();

    /// <summary>
    /// Initializes a new instance of the Mediator class with DI support.
    /// Handlers are resolved from the service provider on each request.
    /// Supports scoped dependencies like DbContext.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving handlers.</param>
    /// <exception cref="ArgumentNullException">Thrown if serviceProvider is null.</exception>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

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
    /// Generates a unique key for an event handler type.
    /// </summary>
    private static string GetEventHandlerKey<TEvent>() where TEvent : IEvent
    => $"event_{typeof(TEvent).FullName}";

    /// <summary>
    /// Registers a command handler type for DI resolution.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    public void RegisterCommandHandler<TCommand>()
        where TCommand : ICommand
    {
        var key = GetCommandHandlerKey<TCommand>();
        _handlerTypeRegistry[key] = typeof(ICommandHandler<TCommand>);
    }

    /// <summary>
    /// Registers a command handler instance (for manual registration or testing).
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="handler">The handler instance.</param>
    public void RegisterCommandHandler<TCommand>(ICommandHandler<TCommand> handler)
where TCommand : ICommand
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var key = GetCommandHandlerKey<TCommand>();
        _handlerTypeRegistry[key] = typeof(ICommandHandler<TCommand>);
    }

    /// <summary>
    /// Registers a command handler type with response for DI resolution.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    public void RegisterCommandHandler<TCommand, TResponse>()
     where TCommand : ICommand<TResponse>
    {
        var key = GetCommandHandlerKey<TCommand, TResponse>();
        _handlerTypeRegistry[key] = typeof(ICommandHandler<TCommand, TResponse>);
    }

    /// <summary>
    /// Registers a command handler instance with response (for manual registration or testing).
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The handler instance.</param>
    public void RegisterCommandHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> handler)
        where TCommand : ICommand<TResponse>
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var key = GetCommandHandlerKey<TCommand, TResponse>();
        _handlerTypeRegistry[key] = typeof(ICommandHandler<TCommand, TResponse>);
    }

    /// <summary>
    /// Registers a query handler type for DI resolution.
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    public void RegisterQueryHandler<TQuery, TResponse>()
           where TQuery : IQuery<TResponse>
    {
        var key = GetQueryHandlerKey<TQuery, TResponse>();
        _handlerTypeRegistry[key] = typeof(IQueryHandler<TQuery, TResponse>);
    }

    /// <summary>
    /// Registers a query handler instance (for manual registration or testing).
    /// </summary>
    /// <typeparam name="TQuery">The query type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="handler">The handler instance.</param>
    public void RegisterQueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> handler)
        where TQuery : IQuery<TResponse>
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var key = GetQueryHandlerKey<TQuery, TResponse>();
        _handlerTypeRegistry[key] = typeof(IQueryHandler<TQuery, TResponse>);
    }

    /// <summary>
    /// Registers an event handler type for DI resolution.
    /// Multiple handlers can be registered for the same event.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    public void RegisterEventHandler<TEvent>()
        where TEvent : IEvent
    {
        var key = GetEventHandlerKey<TEvent>();
        var handlerTypes = _eventHandlerTypeRegistry.GetOrAdd(key, _ => new List<Type>());

        lock (handlerTypes)
        {
            var handlerType = typeof(IEventHandler<TEvent>);
            if (!handlerTypes.Contains(handlerType))
            {
                handlerTypes.Add(handlerType);
            }
        }
    }

    /// <summary>
    /// Registers an event handler instance (for manual registration or testing).
    /// Multiple handlers can be registered for the same event.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="handler">The handler instance.</param>
    public void RegisterEventHandler<TEvent>(IEventHandler<TEvent> handler)
        where TEvent : IEvent
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var key = GetEventHandlerKey<TEvent>();
        var handlerTypes = _eventHandlerTypeRegistry.GetOrAdd(key, _ => new List<Type>());


        lock (handlerTypes)
        {
            var handlerType = typeof(IEventHandler<TEvent>);
            if (!handlerTypes.Contains(handlerType))
            {
                handlerTypes.Add(handlerType);
            }
        }
    }

    /// <summary>
    /// Sends a command without response.
    /// Resolves handler from DI container.
    /// Supports scoped dependencies.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command to send.</param>
    /// <returns>A task that completes when the command is handled.</returns>
    /// <exception cref="HandlerNotRegisteredException">Thrown if no handler is registered.</exception>
    public async Task DispatchAsync<TCommand>(TCommand command) where TCommand : ICommand
    {
        var key = GetCommandHandlerKey<TCommand>();

        if (!_handlerTypeRegistry.TryGetValue(key, out var handlerType))
            throw HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name);

        using(var scope = _serviceProvider.CreateScope())
        {
            var scopedProvider = scope.ServiceProvider;
            var handler = (ICommandHandler<TCommand>)scopedProvider.GetService(handlerType)!;
            if (handler == null)
                throw HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name);
            await handler.Handle(command).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sends a command with response.
    /// Resolves handler from DI container.
    /// Supports scoped dependencies.
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

        if (!_handlerTypeRegistry.TryGetValue(key, out var handlerType))
            throw HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name);

        using (var scope = _serviceProvider.CreateScope())
        {
            var scopedProvider = scope.ServiceProvider;

            var handler = (ICommandHandler<TCommand, TResponse>)scopedProvider.GetService(handlerType)!;
            if (handler == null)
                throw HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name);

            return await handler.Handle(command).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Sends a query.
    /// Resolves handler from DI container.
    /// Supports scoped dependencies.
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

        if (!_handlerTypeRegistry.TryGetValue(key, out var handlerType))
            throw HandlerNotRegisteredException.ForQuery(typeof(TQuery).Name);

        using(var scope = _serviceProvider.CreateScope())
        {
            var scopedProvider = scope.ServiceProvider;

            var handler = (IQueryHandler<TQuery, TResponse>)scopedProvider.GetService(handlerType)!;
            if (handler == null)
                throw HandlerNotRegisteredException.ForQuery(typeof(TQuery).Name);
            return await handler.Handle(query).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Publishes an event to all registered handlers.
    /// Resolves handlers from DI container.
    /// Handlers are executed in parallel.
    /// Each handler gets a fresh scope with its own scoped dependencies.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="event">The event to publish.</param>
    /// <returns>A task that completes when all handlers have completed.</returns>
    public Task PublishAsync<TEvent>(TEvent @event) where TEvent : IEvent
    {
      var key = GetEventHandlerKey<TEvent>();

      if (!_eventHandlerTypeRegistry.TryGetValue(key, out var handlerTypes) || handlerTypes.Count == 0)
  {
            return Task.CompletedTask;
        }

        var tasks = new List<Task>(handlerTypes.Count);

        lock (handlerTypes)
 {
foreach (var handlerType in handlerTypes)
    {
                // Create a task that creates its own scope
                tasks.Add(ExecuteHandlerAsync(handlerType, @event));
            }
     }

    return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
    }

    /// <summary>
    /// Executes a single event handler with its own scope.
/// Ensures each handler gets fresh scoped dependencies like DbContext.
    /// </summary>
    private async Task ExecuteHandlerAsync<TEvent>(Type handlerType, TEvent @event) where TEvent : IEvent
    {
  using (var scope = _serviceProvider.CreateScope())
        {
    var handler = (IEventHandler<TEvent>)scope.ServiceProvider.GetService(handlerType)!;
         if (handler != null)
     {
           await handler.Handle(@event).ConfigureAwait(false);
}
        }
    }