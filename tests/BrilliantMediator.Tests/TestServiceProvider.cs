using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Monbsoft.BrilliantMediator.Tests;

/// <summary>
/// Simple service provider for testing purposes.
/// Allows handlers to be registered and resolved during tests.
/// </summary>
public class TestServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public void AddService<TInterface>(object instance)
    {
        _services[typeof(TInterface)] = instance;
    }

    /// <summary>
    /// Adds a command handler to the service provider.
    /// Automatically registers under the correct interface type.
    /// </summary>
    public void AddCommandHandler<TCommand>(ICommandHandler<TCommand> handler) where TCommand : Abstractions.Commands.ICommand
    {
        _services[typeof(ICommandHandler<TCommand>)] = handler;
    }

    /// <summary>
    /// Adds a command handler with response to the service provider.
    /// Automatically registers under the correct interface type.
    /// </summary>
    public void AddCommandHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> handler)
  where TCommand : Abstractions.Commands.ICommand<TResponse>
    {
     _services[typeof(ICommandHandler<TCommand, TResponse>)] = handler;
    }

    /// <summary>
    /// Adds a query handler to the service provider.
    /// Automatically registers under the correct interface type.
    /// </summary>
    public void AddQueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> handler)
        where TQuery : Abstractions.Queries.IQuery<TResponse>
    {
     _services[typeof(IQueryHandler<TQuery, TResponse>)] = handler;
    }

    /// <summary>
    /// Adds an event handler to the service provider.
    /// Automatically registers under the correct interface type.
    /// </summary>
    public void AddEventHandler<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        _services[typeof(IEventHandler<TEvent>)] = handler;
    }

    public object? GetService(Type serviceType)
    {
        return _services.TryGetValue(serviceType, out var service) ? service : null;
    }
}

/// <summary>
/// Helper to create a Mediator instance with a test service provider.
/// </summary>
public static class TestMediatorFactory
{
    public static (Core.Mediator mediator, TestServiceProvider serviceProvider) Create()
    {
        var serviceProvider = new TestServiceProvider();
 var mediator = new Core.Mediator(serviceProvider);
        return (mediator, serviceProvider);
    }
}
