namespace Monbsoft.BrilliantMediator.Abstractions.Commands;

/// <summary>
/// Handler for a command that does not return a response.
/// </summary>
/// <typeparam name="TCommand">The type of the command.</typeparam>
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    /// <summary>
    /// Handles the command asynchronously.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <returns>A task that completes when the command is handled.</returns>
    Task Handle(TCommand command);
}

/// <summary>
/// Handler for a command that returns a response.
/// </summary>
/// <typeparam name="TCommand">The type of the command.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the command asynchronously and returns a response.
    /// </summary>
    /// <param name="command">The command to handle.</param>
    /// <returns>A task that completes with the response when the command is handled.</returns>
    Task<TResponse> Handle(TCommand command);
}