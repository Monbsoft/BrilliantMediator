using Radiant.ConsoleApp.Application.DTOs;

namespace Radiant.ConsoleApp.Application.Queries;

public class GetAllTodosResult
{
    public List<TodoDto> Todos { get; set; } = new();
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
}