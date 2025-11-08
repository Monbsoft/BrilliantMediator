using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Radiant.ConsoleApp.Application.Commands;

public class CreateTodoCommand : ICommand<CreateTodoResult>
{
    public string Title { get; set; }
    public string Description { get; set; }
}