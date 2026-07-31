using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Pipeline;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Exceptions;
using System.Collections.Concurrent;

namespace Monbsoft.BrilliantMediator.Core;

/// <summary>
/// Ultra-lightweight mediator implementation with DI support.
/// Uses IServiceProvider to resolve handlers on-demand, supporting proper scoping.
/// Supports Commands, Queries, and Events with parallel event handling,
/// plus optional pipeline behaviors around commands and queries.
/// </summary>
public sealed class Mediator : IMediator, IHandlerRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConcurrentDictionary<string, Type> _handlerTypeRegistry = new();
    private readonly ConcurrentDictionary<string, bool> _eventRegistry = new();
    private readonly ConcurrentDictionary<string, bool> _behaviorRegistry = new();

    // ADR-012: first of two guards. Stays false in any application that
    // registers no behavior, so the dispatch path remains the one from v3.0 —
    // no key allocation, no dictionary lookup, no DI resolution.
    private volatile bool _hasPipelineBehaviors;

    /// <summary>
    /// Creates a mediator resolving its handlers from the given service provider.
    /// </summary>
    /// <param name="serviceProvider">The root service provider used to create per-dispatch scopes.</param>
    public Mediator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    #region IHandlerRegistry

    /// <inheritdoc />
    public void RegisterCommandHandler<TCommand>() where TCommand : ICommand
    {
        var key = $"cmd_{typeof(TCommand).FullName}";
        _handlerTypeRegistry[key] = typeof(ICommandHandler<TCommand>);
    }

    /// <inheritdoc />
    public void RegisterCommandHandler<TCommand, TResponse>() where TCommand : ICommand<TResponse>
    {
        var key = $"cmd_resp_{typeof(TCommand).FullName}_{typeof(TResponse).FullName}";
        _handlerTypeRegistry[key] = typeof(ICommandHandler<TCommand, TResponse>);
    }

    /// <inheritdoc />
    public void RegisterQueryHandler<TQuery, TResponse>() where TQuery : IQuery<TResponse>
    {
        var key = $"query_{typeof(TQuery).FullName}_{typeof(TResponse).FullName}";
        _handlerTypeRegistry[key] = typeof(IQueryHandler<TQuery, TResponse>);
    }

    /// <inheritdoc />
    public void RegisterEventHandler<TEvent>() where TEvent : IEvent
    {
        // Presence marker only: concrete handlers are resolved through DI
        // as IEnumerable<IEventHandler<TEvent>>, which supports multiple
        // handlers registered for the same event. Registration is idempotent.
        var key = $"event_{typeof(TEvent).FullName}";
        _eventRegistry.TryAdd(key, true);
    }

    /// <inheritdoc />
    public void RegisterPipelineBehavior<TRequest, TResponse>()
    {
        // Presence marker only, like events: the behaviors themselves are
        // resolved as IEnumerable<IPipelineBehavior<TRequest, TResponse>>.
        _behaviorRegistry.TryAdd(BehaviorKey<TRequest, TResponse>(), true);
        _hasPipelineBehaviors = true;
    }

    /// <inheritdoc />
    public void RegisterPipelineBehavior<TRequest>() where TRequest : ICommand
    {
        _behaviorRegistry.TryAdd(BehaviorKey<TRequest>(), true);
        _hasPipelineBehaviors = true;
    }

    #endregion

    #region IMediator

    /// <inheritdoc />
    public async Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        var key = $"cmd_{typeof(TCommand).FullName}";
        await ExecuteInScopeAsync<TCommand, ICommandHandler<TCommand>>(key, command,
            static () => HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name),
            static (handler, request, ct) => handler.Handle(request, ct),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        var key = $"cmd_resp_{typeof(TCommand).FullName}_{typeof(TResponse).FullName}";
        return await ExecuteInScopeAsync<TCommand, ICommandHandler<TCommand, TResponse>, TResponse>(key, command,
            static () => HandlerNotRegisteredException.ForCommand(typeof(TCommand).Name),
            static (handler, request, ct) => handler.Handle(request, ct),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        var key = $"query_{typeof(TQuery).FullName}_{typeof(TResponse).FullName}";
        return await ExecuteInScopeAsync<TQuery, IQueryHandler<TQuery, TResponse>, TResponse>(key, query,
            static () => HandlerNotRegisteredException.ForQuery(typeof(TQuery).Name),
            static (handler, request, ct) => handler.Handle(request, ct),
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        // ADR-011: events carry no pipeline. PublishAsync is unchanged.
        var key = $"event_{typeof(TEvent).FullName}";

        if (!_eventRegistry.ContainsKey(key))
            return Task.CompletedTask;

        return PublishToAllHandlersAsync(@event, cancellationToken);
    }

    #endregion

    private static string BehaviorKey<TRequest, TResponse>()
        => $"behavior_resp_{typeof(TRequest).FullName}_{typeof(TResponse).FullName}";

    private static string BehaviorKey<TRequest>()
        => $"behavior_{typeof(TRequest).FullName}";

    private async Task ExecuteInScopeAsync<TRequest, THandler>(
        string key,
        TRequest request,
        Func<HandlerNotRegisteredException> exceptionFactory,
        Func<THandler, TRequest, CancellationToken, Task> execute,
        CancellationToken cancellationToken)
        where TRequest : ICommand
        where THandler : class
    {
        if (!_handlerTypeRegistry.TryGetValue(key, out var handlerType))
            throw exceptionFactory();

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService(handlerType) as THandler
            ?? throw exceptionFactory();

        if (!_hasPipelineBehaviors)
        {
            await execute(handler, request, cancellationToken).ConfigureAwait(false);
            return;
        }

        await ExecuteWithBehaviorsAsync(scope, handler, request, execute, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TResult> ExecuteInScopeAsync<TRequest, THandler, TResult>(
        string key,
        TRequest request,
        Func<HandlerNotRegisteredException> exceptionFactory,
        Func<THandler, TRequest, CancellationToken, Task<TResult>> execute,
        CancellationToken cancellationToken) where THandler : class
    {
        if (!_handlerTypeRegistry.TryGetValue(key, out var handlerType))
            throw exceptionFactory();

        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService(handlerType) as THandler
            ?? throw exceptionFactory();

        // ADR-012: single volatile read on the path without behaviors.
        if (!_hasPipelineBehaviors)
            return await execute(handler, request, cancellationToken).ConfigureAwait(false);

        return await ExecuteWithBehaviorsAsync(scope, handler, request, execute, cancellationToken).ConfigureAwait(false);
    }

    private async Task ExecuteWithBehaviorsAsync<TRequest, THandler>(
        IServiceScope scope,
        THandler handler,
        TRequest request,
        Func<THandler, TRequest, CancellationToken, Task> execute,
        CancellationToken cancellationToken)
        where TRequest : ICommand
        where THandler : class
    {
        // ADR-012: second guard. Behaviors exist somewhere, but maybe not for
        // this request — no DI resolution in that case.
        if (!_behaviorRegistry.ContainsKey(BehaviorKey<TRequest>()))
        {
            await execute(handler, request, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Behaviors share the handler's scope (ADR-002), so a scoped dependency
        // such as a DbContext is the same instance throughout the pipeline.
        var resolved = scope.ServiceProvider.GetService<IEnumerable<IPipelineBehavior<TRequest>>>();
        var behaviors = resolved as IPipelineBehavior<TRequest>[] ?? resolved?.ToArray();

        if (behaviors is null || behaviors.Length == 0)
        {
            await execute(handler, request, cancellationToken).ConfigureAwait(false);
            return;
        }

        RequestHandlerDelegate next = () => execute(handler, request, cancellationToken);

        // Built inside out so that index 0 — the first registered — ends up
        // outermost (ADR-008).
        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var inner = next;
            next = () => behavior.Handle(request, inner, cancellationToken);
        }

        await next().ConfigureAwait(false);
    }

    private async Task<TResult> ExecuteWithBehaviorsAsync<TRequest, THandler, TResult>(
        IServiceScope scope,
        THandler handler,
        TRequest request,
        Func<THandler, TRequest, CancellationToken, Task<TResult>> execute,
        CancellationToken cancellationToken) where THandler : class
    {
        if (!_behaviorRegistry.ContainsKey(BehaviorKey<TRequest, TResult>()))
            return await execute(handler, request, cancellationToken).ConfigureAwait(false);

        var resolved = scope.ServiceProvider.GetService<IEnumerable<IPipelineBehavior<TRequest, TResult>>>();
        var behaviors = resolved as IPipelineBehavior<TRequest, TResult>[] ?? resolved?.ToArray();

        if (behaviors is null || behaviors.Length == 0)
            return await execute(handler, request, cancellationToken).ConfigureAwait(false);

        RequestHandlerDelegate<TResult> next = () => execute(handler, request, cancellationToken);

        for (var i = behaviors.Length - 1; i >= 0; i--)
        {
            var behavior = behaviors[i];
            var inner = next;
            next = () => behavior.Handle(request, inner, cancellationToken);
        }

        return await next().ConfigureAwait(false);
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
