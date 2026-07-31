using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Radiant.ConsoleApp.Application.DTOs;

namespace Radiant.ConsoleApp.Application.Queries;

// A missing id is a normal outcome of a lookup, not an error: the response type
// is nullable so the handler can report "not found" without throwing.
public class GetTodoByIdQuery : IQuery<TodoDto?>
{
    public int TodoId { get; set; }
}