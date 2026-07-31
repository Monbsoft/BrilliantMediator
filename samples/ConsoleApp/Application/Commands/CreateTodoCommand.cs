using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Radiant.ConsoleApp.Application.Commands;

public class CreateTodoCommand : ICommand<CreateTodoResult>
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}