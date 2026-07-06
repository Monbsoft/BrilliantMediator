using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Exceptions;
using System.Collections.Concurrent;

namespace Monbsoft.BrilliantMediator.Core;

/// <summary>
/// Ultra-lightweight mediator implementation with DI support.
/// Uses IServiceProvider to resolve handlers on-demand, supporting proper scoping.
/// Supports Commands, Queries, and Events with parallel event handling.
/// </summary>
public sealed class Mediator : IMediator, IHandlerRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, Type> _handlerTypeRegistry = new();
    private readonly ConcurrentDictionary<string, bool> _eventRegistry = new();

    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    #region IHandlerRegistry

    public void RegisterCommandHandler<TCommand>() where TCommand : ICommand
    {
        var key = $"cmd_{typeof(TCommand).FullName}";
        _handlerTypeRegistry[key] = typeof(ICommandHandler<TCommand>);
    }

    public void RegisterCommandHandler<TCommand, TResponse>() where TCommand : ICommand<TResponse>
    {
        var key = $"cmd_resp_{typeof(TCommand).FullName}_{typeof(TResponse).FullName}";
        _handlerTypeRegistry[key] = typeof(ICommandHandler<TCommand, TResponse>);
    }

    public void RegisterQueryHandler<TQuery, TResponse>() where TQuery : IQuery<TResponse>
    {
        var key = $"query_{typeof(TQuery).FullName}_{typeof(TResponse).FullName}";
        _handlerTypeRegistry[key] = typeof(IQueryHandler<TQuery, TResponse>);
    }

    public void RegisterEventHandler<TEvent>() where TEvent : IEvent
    {
        // Presence marker only: concrete handlers are resolved through DI
        // as IEnumerable<IEventHandler<TEvent>>, which supports multiple
        // handlers registered for the same event. Registration is idempotent.
        var key = $"event_{typeof(TEvent).FullName}";
        _eventRegistry.TryAdd(key, true);
    }

    #endregion

    #region IMediator

    public async Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        var key = $"cmd_{typeof(TCommand).FullName}";
        await ExecuteInScopeAsync<ICommandHandler<TCommand>>(key,
            () => HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name),
            (handler, ct) => handler.Handle(command, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        var key = $"cmd_resp_{typeof(TCommand).FullName}_{typeof(TResponse).FullName}";
        return await ExecuteInScopeAsync<ICommandHandler<TCommand, TResponse>, TResponse>(key,
            () => HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name),
            (handler, ct) => handler.Handle(command, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        var key = $"query_{typeof(TQuery).FullName}_{typeof(TResponse).FullName}";
        return await ExecuteInScopeAsync<IQueryHandler<TQuery, TResponse>, TResponse>(key,
            () => HandlerNotRegisteredException.ForQuery(typeof(TQuery).Name),
            (handler, ct) => handler.Handle(query, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        var key = $"event_{typeof(TEvent).FullName}";

        if (!_eventRegistry.ContainsKey(key))
            return Task.CompletedTask;

        return PublishToAllHandlersAsync(@event, cancellationToken);
    }

    #endregion

    private async Task ExecuteInScopeAsync<THandler>(
        string key,
        Func<HandlerNotRegisteredException> exceptionFactory,
        Func<THandler, CancellationToken, Task> execute,
        CancellationToken cancellationToken) where THandler : class
    {
        if (!_handlerTypeRegistry.TryGetValue(key, out var handlerType))
            throw exceptionFactory();

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService(handlerType) as THandler
            ?? throw exceptionFactory();
        await execute(handler, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResult> ExecuteInScopeAsync<THandler, TResult>(
        string key,
        Func<HandlerNotRegisteredException> exceptionFactory,
        Func<THandler, CancellationToken, Task<TResult>> execute,
        CancellationToken cancellationToken) where THandler : class
    {
        if (!_handlerTypeRegistry.TryGetValue(key, out var handlerType))
            throw exceptionFactory();

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService(handlerType) as THandler
            ?? throw exceptionFactory();
        return await execute(handler, cancellationToken).ConfigureAwait(false);
    }

    private async Task PublishToAllHandlersAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        // ADR-002: each handler runs in its own DI scope. A dedicated counting
        // scope determines how many handlers are registered before fanning out.
        int handlerCount;
        using (var countingScope = _serviceProvider.CreateScope())
        {
            handlerCount = countingScope.ServiceProvider
                .GetService<IEnumerable<IEventHandler<TEvent>>>()?.Count() ?? 0;
        }

        if (handlerCount == 0)
            throw HandlerNotRegisteredException.ForEvent(typeof(TEvent).Name);

        var tasks = new Task[handlerCount];
        for (var i = 0; i < handlerCount; i++)
        {
            tasks[i] = ExecuteEventHandlerAsync(i, @event, cancellationToken);
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ExecuteEventHandlerAsync<TEvent>(int handlerIndex, TEvent @event, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        // Index-based resolution assumes IEnumerable<T> ordering is stable across
        // scopes: MS.DI resolves collections in registration order, but this is
        // not guaranteed by the general IServiceProvider contract.
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider
            .GetService<IEnumerable<IEventHandler<TEvent>>>()?.ElementAtOrDefault(handlerIndex)
            ?? throw HandlerNotRegisteredException.ForEvent(typeof(TEvent).Name);
        await handler.Handle(@event, cancellationToken).ConfigureAwait(false);
    }
}
