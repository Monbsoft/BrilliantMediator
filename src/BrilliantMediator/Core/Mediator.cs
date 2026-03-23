using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Exceptions;
using System.Collections.Concurrent;
using System.Collections.Immutable;

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
    private readonly ConcurrentDictionary<string, ImmutableList<Type>> _eventHandlerTypeRegistry = new();

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
        var key = $"event_{typeof(TEvent).FullName}";
        var handlerType = typeof(IEventHandler<TEvent>);

        _eventHandlerTypeRegistry.AddOrUpdate(
            key,
            _ => ImmutableList.Create(handlerType),
            (_, existing) => existing.Contains(handlerType) ? existing : existing.Add(handlerType));
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

        if (!_eventHandlerTypeRegistry.TryGetValue(key, out var handlerTypes) || handlerTypes.IsEmpty)
            return Task.CompletedTask;

        // ImmutableList is thread-safe for reads — no lock needed
        var tasks = new Task[handlerTypes.Count];
        for (var i = 0; i < handlerTypes.Count; i++)
        {
            tasks[i] = ExecuteEventHandlerAsync(handlerTypes[i], @event, cancellationToken);
        }

        return Task.WhenAll(tasks);
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

    private async Task ExecuteEventHandlerAsync<TEvent>(Type handlerType, TEvent @event, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService(handlerType) as IEventHandler<TEvent>
            ?? throw HandlerNotRegisteredException.ForEvent(typeof(TEvent).Name);
        await handler.Handle(@event, cancellationToken).ConfigureAwait(false);
    }
}
