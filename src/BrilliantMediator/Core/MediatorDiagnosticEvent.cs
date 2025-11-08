namespace BrilliantMediator.Core;

/// <summary>
/// Represents a diagnostic event from the mediator.
/// </summary>
public sealed class MediatorDiagnosticEvent
{
    /// <summary>
    /// Gets the type of the event.
    /// </summary>
    public MediatorEventType EventType { get; set; }

    /// <summary>
    /// Gets the timestamp of the event.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets the name of the command or query that was not found.
    /// </summary>
    public string? HandlerName { get; set; }

    /// <summary>
    /// Gets the message of the event.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets the exception associated with the event, if any.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Creates a new diagnostic event.
    /// </summary>
    public static MediatorDiagnosticEvent CreateHandlerNotFound(string handlerName)
    {
        return new MediatorDiagnosticEvent
        {
            EventType = MediatorEventType.HandlerNotFound,
            Timestamp = DateTime.UtcNow,
            HandlerName = handlerName,
            Message = $"No handler registered for '{handlerName}'"
        };
    }

    /// <summary>
    /// Creates a new diagnostic event for successful handler execution.
    /// </summary>
    public static MediatorDiagnosticEvent CreateHandlerExecuted(string handlerName)
    {
        return new MediatorDiagnosticEvent
        {
            EventType = MediatorEventType.HandlerExecuted,
            Timestamp = DateTime.UtcNow,
            HandlerName = handlerName,
            Message = $"Handler '{handlerName}' executed successfully"
        };
    }

    /// <summary>
    /// Creates a new diagnostic event for handler registration.
    /// </summary>
    public static MediatorDiagnosticEvent CreateHandlerRegistered(string handlerName)
    {
        return new MediatorDiagnosticEvent
        {
            EventType = MediatorEventType.HandlerRegistered,
            Timestamp = DateTime.UtcNow,
            HandlerName = handlerName,
            Message = $"Handler '{handlerName}' registered"
        };
    }
}


