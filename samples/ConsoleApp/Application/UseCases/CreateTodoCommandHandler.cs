using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Radiant.ConsoleApp.Application.Commands;
using Radiant.ConsoleApp.Application.Domain;
using Radiant.ConsoleApp.Application.Interfaces;

namespace Radiant.ConsoleApp.Application.UseCases;

public class CreateTodoCommandHandler : ICommandHandler<CreateTodoCommand, CreateTodoResult>
{
    private readonly ITodoRepository _repository;

    public CreateTodoCommandHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateTodoResult> Handle(CreateTodoCommand command, CancellationToken cancellationToken = default)
    {
        var todo = new Todo
        {
            Title = command.Title,
            Description = command.Description,
            Status = TodoStatus.Pending
        };

        await _repository.AddAsync(todo);

        return new CreateTodoResult
        {
            TodoId = todo.Id,
            Success = true,
            Message = $"Todo '{command.Title}' created successfully"
        };
    }
}