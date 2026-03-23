namespace Monbsoft.BrilliantMediator.Abstractions.Events;

/// <summary>
/// Handler for domain events.
/// Multiple handlers can be registered for the same event.
/// Handlers are executed in parallel.
/// </summary>
/// <typeparam name="TEvent">The type of the event.</typeparam>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// Handles the event asynchronously.
    /// </summary>
    /// <param name="event">The event to handle.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the event is handled.</returns>
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}
