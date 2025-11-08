using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Radiant.ConsoleApp.Application.Commands;
using Radiant.ConsoleApp.Application.Domain;
using Radiant.ConsoleApp.Application.Interfaces;

namespace Radiant.ConsoleApp.Application.UseCases;

public class CompleteTodoCommandHandler : ICommandHandler<CompleteTodoCommand, CompleteTodoResult>
{
    private readonly ITodoRepository _repository;

    public CompleteTodoCommandHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<CompleteTodoResult> Handle(CompleteTodoCommand command)
    {
        var todo = await _repository.GetByIdAsync(command.TodoId);
        if (todo == null)
            return new CompleteTodoResult
            {
                Success = false,
                Message = $"Todo {command.TodoId} not found"
            };

        todo.Status = TodoStatus.Completed;
        todo.CompletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(todo);

        var completedCount = await _repository.GetCompletedCountAsync();

        return new CompleteTodoResult
        {
            Success = true,
            Message = "Todo completed successfully",
            CompletedCount = completedCount
        };
    }
}