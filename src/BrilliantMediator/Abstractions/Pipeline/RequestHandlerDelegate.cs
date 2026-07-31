namespace Monbsoft.BrilliantMediator.Abstractions.Pipeline;

/// <summary>
/// Represents the next step of the pipeline for a request that returns a response.
/// Invoking it runs the remaining behaviors and, last, the request handler.
/// Not invoking it short-circuits the pipeline (ADR-009).
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <returns>A task that completes with the response produced by the rest of the pipeline.</returns>
public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();

/// <summary>
/// Represents the next step of the pipeline for a request without response.
/// Invoking it runs the remaining behaviors and, last, the request handler.
/// Not invoking it short-circuits the pipeline (ADR-009).
/// </summary>
/// <returns>A task that completes when the rest of the pipeline has run.</returns>
public delegate Task RequestHandlerDelegate();
