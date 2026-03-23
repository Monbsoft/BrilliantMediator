namespace Monbsoft.BrilliantMediator.Exceptions;

/// <summary>
/// Thrown when a handler is not registered for a command, query, or event.
/// </summary>
public sealed class HandlerNotRegisteredException : Exception
{
    public HandlerNotRegisteredException(string message) : base(message)
    {
    }

    public static HandlerNotRegisteredException ForCommand(string commandName)
        => new($"No handler registered for command '{commandName}'");

    public static HandlerNotRegisteredException ForQuery(string queryName)
        => new($"No handler registered for query '{queryName}'");

    public static HandlerNotRegisteredException ForEvent(string eventName)
        => new($"No handler registered for event '{eventName}'");
}
