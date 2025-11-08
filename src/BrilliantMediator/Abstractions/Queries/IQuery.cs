namespace Monbsoft.BrilliantMediator.Abstractions.Queries;

/// <summary>
/// Represents a query that returns a response of type TResponse.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IQuery<out TResponse>
{ }