namespace Monbsoft.BrilliantMediator.Exceptions;

/// <summary>
/// Thrown when a handler is not registered for a command or query.
/// </summary>
public sealed class HandlerNotRegisteredException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HandlerNotRegisteredException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public HandlerNotRegisteredException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new exception for a command.
    /// </summary>
    /// <param name="commandName">The name of the command.</param>
    /// <returns>A new exception instance.</returns>
    public static HandlerNotRegisteredException ForCommand(string commandName)
    {
        return new($"No handler registered for command '{commandName}'");
    }

    /// <summary>
    /// Creates a new exception for a query.
    /// </summary>
    /// <param name="queryName">The name of the query.</param>
    /// <returns>A new exception instance.</returns>
    public static HandlerNotRegisteredException ForQuery(string queryName)
    {
        return new($"No handler registered for query '{queryName}'");
    }
}
