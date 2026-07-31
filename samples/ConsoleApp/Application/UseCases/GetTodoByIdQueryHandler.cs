using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Radiant.ConsoleApp.Application.DTOs;
using Radiant.ConsoleApp.Application.Interfaces;
using Radiant.ConsoleApp.Application.Queries;

namespace Radiant.ConsoleApp.Application.UseCases;

public class GetTodoByIdQueryHandler : IQueryHandler<GetTodoByIdQuery, TodoDto>
{
    private readonly ITodoRepository _repository;

    public GetTodoByIdQueryHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<TodoDto> Handle(GetTodoByIdQuery query, CancellationToken cancellationToken = default)
    {
        var todo = await _repository.GetByIdAsync(query.TodoId);
        if (todo == null)
            throw new KeyNotFoundException($"No todo found with id {query.TodoId}");

        return new TodoDto
        {
            Id = todo.Id,
            Title = todo.Title,
            Description = todo.Description,
            Status = todo.Status.ToString(),
            CreatedAt = todo.CreatedAt,
            CompletedAt = todo.CompletedAt
        };
    }
}