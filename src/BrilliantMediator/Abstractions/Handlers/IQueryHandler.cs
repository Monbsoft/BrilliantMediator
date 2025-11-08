using BrilliantMediator.Abstractions.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrilliantMediator.Abstractions.Handlers;

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
    /// <returns>A task that completes with the response when the query is handled.</returns>
    Task<TResponse> Handle(TQuery query);
}

