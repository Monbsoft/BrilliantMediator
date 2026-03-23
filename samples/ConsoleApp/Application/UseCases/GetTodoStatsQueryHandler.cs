using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Radiant.ConsoleApp.Application.Domain;
using Radiant.ConsoleApp.Application.Interfaces;
using Radiant.ConsoleApp.Application.Queries;

namespace Radiant.ConsoleApp.Application.UseCases;

public class GetTodoStatsQueryHandler : IQueryHandler<GetTodoStatsQuery, TodoStats>
{
    private readonly ITodoRepository _repository;

    public GetTodoStatsQueryHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<TodoStats> Handle(GetTodoStatsQuery query, CancellationToken cancellationToken = default)
    {
        var todos = await _repository.GetAllAsync();
        var total = todos.Count;
        var completed = todos.Count(t => t.Status == TodoStatus.Completed);
        var inProgress = todos.Count(t => t.Status == TodoStatus.InProgress);
        var pending = todos.Count(t => t.Status == TodoStatus.Pending);

        return new TodoStats
        {
            Total = total,
            Completed = completed,
            InProgress = inProgress,
            Pending = pending,
            CompletionPercentage = total > 0 ? (decimal)completed / total * 100 : 0
        };
    }
}