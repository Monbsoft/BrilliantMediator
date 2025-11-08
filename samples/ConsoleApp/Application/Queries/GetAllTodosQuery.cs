using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Radiant.ConsoleApp.Application.Domain;

namespace Radiant.ConsoleApp.Application.Queries;

public class GetAllTodosQuery : IQuery<GetAllTodosResult>
{
    public TodoStatus? FilterByStatus { get; set; }
}