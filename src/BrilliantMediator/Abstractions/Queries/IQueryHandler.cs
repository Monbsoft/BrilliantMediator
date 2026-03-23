namespace Monbsoft.BrilliantMediator.Abstractions.Queries;

/// <summary>
/// Handler for a query.
/// </summary>
/// <typeparam name="TQuery">The type of the query.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles the query asynchronously and returns a response.
    /// </summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes with the response when the query is handled.</returns>
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default);
}
