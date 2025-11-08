using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Monbsoft.BrilliantMediator.Abstractions;

public interface IMediator
{
    void RegisterCommandHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> handler) where TCommand : ICommand<TResponse>;
    void RegisterCommandHandler<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand;
    void RegisterQueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> handler) where TQuery : IQuery<TResponse>;
    Task DispatchAsync<TCommand>(TCommand command) where TCommand : ICommand;
    Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command) where TCommand : ICommand<TResponse>;
    Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query) where TQuery : IQuery<TResponse>;
}