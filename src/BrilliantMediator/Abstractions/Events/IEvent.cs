namespace Monbsoft.BrilliantMediator.Abstractions.Events;

/// <summary>
/// Marker interface for domain events.
/// Events represent something that has happened in the domain.
/// Multiple handlers can process the same event.
/// Zero-reflection, compile-time type-safe.
/// </summary>
public interface IEvent
{
}
