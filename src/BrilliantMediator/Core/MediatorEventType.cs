namespace Monbsoft.BrilliantMediator.Core;

/// <summary>
/// Enumeration of diagnostic event types.
/// </summary>
public enum MediatorEventType
{
    /// <summary>
    /// A handler was not found.
    /// </summary>
    HandlerNotFound,

    /// <summary>
    /// A handler was successfully executed.
    /// </summary>
    HandlerExecuted,

    /// <summary>
    /// A handler was registered.
    /// </summary>
    HandlerRegistered,

    /// <summary>
    /// An error occurred.
    /// </summary>
    Error
}