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

    /// <summary>
    /// Marks a request/response pair as having at least one pipeline behavior.
    /// The behaviors themselves are resolved from DI at dispatch time, but this
    /// marker <b>gates that resolution</b>: a request with no marker never has its
    /// pipeline resolved, so behaviors present in the container are not executed
    /// (ADR-012). Registration is idempotent.
    /// </summary>
    /// <remarks>
    /// The marker and the DI registration must therefore stay in step.
    /// <see cref="Extensions.MediatorBuilder.AddPipelineBehavior{TRequest, TResponse, TBehavior}"/>
    /// does both in one call and is the supported way to register a behavior.
    /// Adding an <c>IPipelineBehavior</c> directly to the service collection, or
    /// calling <c>AddPipelineBehavior</c> after <c>Build()</c>, leaves the marker
    /// unset: the behavior resolves fine yet never runs, silently.
    /// </remarks>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    void RegisterPipelineBehavior<TRequest, TResponse>();

    /// <summary>
    /// Marks a command without response as having at least one pipeline behavior.
    /// Like its two-parameter counterpart, this marker gates pipeline resolution:
    /// without it the command's behaviors are never executed. Registration is idempotent.
    /// </summary>
    /// <typeparam name="TRequest">The command type.</typeparam>
    void RegisterPipelineBehavior<TRequest>()
        where TRequest : ICommand;
}
