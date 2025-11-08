using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Radiant.ConsoleApp.Application.Commands;
using Radiant.ConsoleApp.Application.Interfaces;

namespace Radiant.ConsoleApp.Application.UseCases;

public class DeleteTodoCommandHandler : ICommandHandler<DeleteTodoCommand>
{
    private readonly ITodoRepository _repository;

    public DeleteTodoCommandHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteTodoCommand command)
    {
        await _repository.DeleteAsync(command.TodoId);
    }
}