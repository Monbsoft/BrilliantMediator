namespace Monbsoft.BrilliantMediator.Abstractions.Pipeline;

/// <summary>
/// Wraps the execution of a request that returns a response — a query, or a
/// command with response. Behaviors compose into a chain around the handler:
/// the first behavior registered is the outermost one (ADR-008).
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <remarks>
/// The behavior class itself may be generic; only its DI registration must be
/// closed by the compiler, which keeps resolution reflection-free (ADR-010).
/// </remarks>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    /// <summary>
    /// Handles the request, optionally invoking the rest of the pipeline.
    /// </summary>
    /// <param name="request">The request being dispatched.</param>
    /// <param name="next">The next step of the pipeline. Not calling it short-circuits the handler.</param>
    /// <param name="cancellationToken">The token passed by the caller of the mediator.</param>
    /// <returns>A task that completes with the response.</returns>
    Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken);
}

/// <summary>
/// Wraps the execution of a command that returns no response. Behaviors compose
/// into a chain around the handler: the first behavior registered is the
/// outermost one (ADR-008).
/// </summary>
/// <typeparam name="TRequest">The type of the command.</typeparam>
/// <remarks>
/// A separate interface is used instead of a public <c>Unit</c> sentinel type,
/// mirroring the arity split already applied to <c>ICommand</c> and
/// <c>ICommandHandler</c> (ADR-007).
/// </remarks>
public interface IPipelineBehavior<in TRequest>
{
    /// <summary>
    /// Handles the command, optionally invoking the rest of the pipeline.
    /// </summary>
    /// <param name="request">The command being dispatched.</param>
    /// <param name="next">The next step of the pipeline. Not calling it short-circuits the handler.</param>
    /// <param name="cancellationToken">The token passed by the caller of the mediator.</param>
    /// <returns>A task that completes when the pipeline has run.</returns>
    Task Handle(
        TRequest request,
        RequestHandlerDelegate next,
        CancellationToken cancellationToken);
}
