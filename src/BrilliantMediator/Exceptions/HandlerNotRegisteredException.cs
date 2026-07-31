namespace Monbsoft.BrilliantMediator.Exceptions;

/// <summary>
/// Thrown when a handler is not registered for a command, query, or event.
/// </summary>
public sealed class HandlerNotRegisteredException : Exception
{
    /// <summary>
    /// Creates the exception with an explicit message.
    /// </summary>
    /// <param name="message">The message describing the missing registration.</param>
    public HandlerNotRegisteredException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates the exception for a command with no registered handler.
    /// </summary>
    /// <param name="commandName">The name of the command type.</param>
    /// <returns>The exception to throw.</returns>
    public static HandlerNotRegisteredException ForCommand(string commandName)
        => new($"No handler registered for command '{commandName}'");

    /// <summary>
    /// Creates the exception for a query with no registered handler.
    /// </summary>
    /// <param name="queryName">The name of the query type.</param>
    /// <returns>The exception to throw.</returns>
    public static HandlerNotRegisteredException ForQuery(string queryName)
        => new($"No handler registered for query '{queryName}'");

    /// <summary>
    /// Creates the exception for an event with no registered handler.
    /// </summary>
    /// <param name="eventName">The name of the event type.</param>
    /// <returns>The exception to throw.</returns>
    public static HandlerNotRegisteredException ForEvent(string eventName)
        => new($"No handler registered for event '{eventName}'");
}
