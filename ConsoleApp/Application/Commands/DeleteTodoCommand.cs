using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Radiant.ConsoleApp.Application.Commands;

public class DeleteTodoCommand : ICommand
{
    public int TodoId { get; set; }
}