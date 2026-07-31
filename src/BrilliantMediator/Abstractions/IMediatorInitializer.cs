namespace Monbsoft.BrilliantMediator.Abstractions;

/// <summary>
/// Initializes the mediator by registering all handlers with the handler registry.
/// </summary>
public interface IMediatorInitializer
{
    /// <summary>
    /// Replays every handler and behavior registration collected by the builder
    /// against the given registry.
    /// </summary>
    /// <param name="registry">The registry to populate, implemented by the mediator.</param>
    void Initialize(IHandlerRegistry registry);
}
