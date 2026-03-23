using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Monbsoft.BrilliantMediator.Tests;

/// <summary>
/// Simple service provider for testing purposes.
/// Allows handlers to be registered and resolved during tests.
/// Supports scoping for mediator operations.
/// </summary>
public class TestServiceProvider : IServiceProvider, IServiceScopeFactory
{
    private readonly Dictionary<Type, object> _services = new();

    public TestServiceProvider()
    {
        _services[typeof(IServiceScopeFactory)] = this;
    }

    public void AddService<TInterface>(object instance)
    {
        _services[typeof(TInterface)] = instance;
    }

    public void AddCommandHandler<TCommand>(ICommandHandler<TCommand> handler) where TCommand : ICommand
    {
        _services[typeof(ICommandHandler<TCommand>)] = handler;
    }

    public void AddCommandHandler<TCommand, TResponse>(ICommandHandler<TCommand, TResponse> handler)
        where TCommand : ICommand<TResponse>
    {
        _services[typeof(ICommandHandler<TCommand, TResponse>)] = handler;
    }

    public void AddQueryHandler<TQuery, TResponse>(IQueryHandler<TQuery, TResponse> handler)
        where TQuery : IQuery<TResponse>
    {
        _services[typeof(IQueryHandler<TQuery, TResponse>)] = handler;
    }

    public void AddEventHandler<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        _services[typeof(IEventHandler<TEvent>)] = handler;
    }

    public object? GetService(Type serviceType)
    {
        return _services.TryGetValue(serviceType, out var service) ? service : null;
    }

    public IServiceScope CreateScope()
    {
        return new TestServiceScope(this);
    }

    private class TestServiceScope : IServiceScope
    {
        public TestServiceScope(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }

        public void Dispose()
        {
        }
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
