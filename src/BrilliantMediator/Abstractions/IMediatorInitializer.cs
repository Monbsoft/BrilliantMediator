namespace Monbsoft.BrilliantMediator.Abstractions;

/// <summary>
/// Initializes the mediator by registering all handlers with the handler registry.
/// </summary>
public interface IMediatorInitializer
{
    void Initialize(IHandlerRegistry registry);
}
