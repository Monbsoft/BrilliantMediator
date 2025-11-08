using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Radiant.ConsoleApp.Application.DTOs;

namespace Radiant.ConsoleApp.Application.Queries;

public class GetTodoByIdQuery : IQuery<TodoDto>
{
    public int TodoId { get; set; }
}