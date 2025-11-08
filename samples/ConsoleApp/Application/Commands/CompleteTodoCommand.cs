using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Radiant.ConsoleApp.Application.Commands;

public class CompleteTodoCommand : ICommand<CompleteTodoResult>
{
    public int TodoId { get; set; }
}