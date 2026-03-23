using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Radiant.ConsoleApp.Application.Domain;
using Radiant.ConsoleApp.Application.DTOs;
using Radiant.ConsoleApp.Application.Interfaces;
using Radiant.ConsoleApp.Application.Queries;

namespace Radiant.ConsoleApp.Application.UseCases;

// ============================================================================
// QUERY HANDLERS
// ============================================================================

public class GetAllTodosQueryHandler : IQueryHandler<GetAllTodosQuery, GetAllTodosResult>
{
    private readonly ITodoRepository _repository;

    public GetAllTodosQueryHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllTodosResult> Handle(GetAllTodosQuery query, CancellationToken cancellationToken = default)
    {
        var todos = await _repository.GetAllAsync();

        if (query.FilterByStatus.HasValue)
        {
            todos = todos.FindAll(t => t.Status == query.FilterByStatus.Value);
        }

        var completedCount = todos.Count(t => t.Status == TodoStatus.Completed);

        return new GetAllTodosResult
        {
            Todos = todos.ConvertAll(t => new TodoDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt
            }),
            TotalCount = todos.Count,
            CompletedCount = completedCount
        };
    }
}