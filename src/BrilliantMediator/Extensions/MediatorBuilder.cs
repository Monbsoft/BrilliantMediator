using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Monbsoft.BrilliantMediator.Extensions;

/// <summary>
/// Builder for fluent registration of handlers.
/// Zero reflection - all at compile-time with generics.
/// </summary>
public sealed class MediatorBuilder
{
    private readonly IServiceCollection _services;
    private readonly List<Action<IServiceProvider, IMediator>> _handlerRegistrations = new();

    public MediatorBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Registers a command handler without response.
    /// Zero reflection, compile-time safe.
    /// </summary>
    public MediatorBuilder AddCommandHandler<TCommand, THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        _services.Add(new ServiceDescriptor(
        typeof(ICommandHandler<TCommand>),
          typeof(THandler),
  lifetime));

      // Store registration action for later execution
        _handlerRegistrations.Add((provider, mediator) =>
        {
          mediator.RegisterCommandHandler<TCommand>();
        });

        return this;
    }

    /// <summary>
    /// Registers a command handler with response.
    /// Zero reflection, compile-time safe.
    /// </summary>
    public MediatorBuilder AddCommandHandler<TCommand, TResponse, THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
 where TCommand : ICommand<TResponse>
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        _services.Add(new ServiceDescriptor(
       typeof(ICommandHandler<TCommand, TResponse>),
          typeof(THandler),
             lifetime));

        // Store registration action for later execution
     _handlerRegistrations.Add((provider, mediator) =>
        {
        mediator.RegisterCommandHandler<TCommand, TResponse>();
     });

        return this;
    }

    /// <summary>
    /// Registers a query handler.
    /// Zero reflection, compile-time safe.
    /// </summary>
    public MediatorBuilder AddQueryHandler<TQuery, TResponse, THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
    where TQuery : IQuery<TResponse>
      where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        _services.Add(new ServiceDescriptor(
     typeof(IQueryHandler<TQuery, TResponse>),
     typeof(THandler),
            lifetime));

        // Store registration action for later execution
        _handlerRegistrations.Add((provider, mediator) =>
        {
    mediator.RegisterQueryHandler<TQuery, TResponse>();
        });

        return this;
 }

    /// <summary>
    /// Registers an event handler.
    /// Zero reflection, compile-time safe.
    /// Multiple handlers can be registered for the same event.
    /// </summary>
    public MediatorBuilder AddEventHandler<TEvent, THandler>(ServiceLifetime lifetime = ServiceLifetime.Scoped)
    where TEvent : IEvent
      where THandler : class, IEventHandler<TEvent>
    {
     _services.Add(new ServiceDescriptor(
   typeof(IEventHandler<TEvent>),
      typeof(THandler),
            lifetime));

        // Store registration action for later execution
        _handlerRegistrations.Add((provider, mediator) =>
     {
            mediator.RegisterEventHandler<TEvent>();
        });

        return this;
    }

 // Delegate-based handler implementations for registration
  private sealed class DelegateCommandHandler<TCommand> : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        private readonly Func<TCommand, Task> _handler;
        public DelegateCommandHandler(Func<TCommand, Task> handler) => _handler = handler;
        public Task Handle(TCommand command) => _handler(command);
    }

    private sealed class DelegateCommandHandler<TCommand, TResponse> : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
      private readonly Func<TCommand, Task<TResponse>> _handler;
        public DelegateCommandHandler(Func<TCommand, Task<TResponse>> handler) => _handler = handler;
        public Task<TResponse> Handle(TCommand command) => _handler(command);
    }

    private sealed class DelegateQueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        private readonly Func<TQuery, Task<TResponse>> _handler;
        public DelegateQueryHandler(Func<TQuery, Task<TResponse>> handler) => _handler = handler;
        public Task<TResponse> Handle(TQuery query) => _handler(query);
    }

    private sealed class DelegateEventHandler<TEvent> : IEventHandler<TEvent>
 where TEvent : IEvent
    {
        private readonly Func<TEvent, Task> _handler;
     public DelegateEventHandler(Func<TEvent, Task> handler) => _handler = handler;
public Task Handle(TEvent @event) => _handler(@event);
    }

    /// <summary>
    /// Finalizes the builder and returns the service collection.
    /// </summary>
    public IServiceCollection Build()
    {
        // Register a post-initialization service that will configure the mediator
        _services.AddSingleton<IMediatorInitializer>(provider =>
    {
          return new MediatorInitializer(_handlerRegistrations, provider);
      });

        return _services;
    }
}

/// <summary>
/// Interface for mediator initialization
/// </summary>
public interface IMediatorInitializer
{
    void Initialize(IMediator mediator);
}

/// <summary>
/// Implementation for mediator initialization
/// </summary>
internal class MediatorInitializer : IMediatorInitializer
{
    private readonly List<Action<IServiceProvider, IMediator>> _registrations;
    private readonly IServiceProvider _serviceProvider;

    public MediatorInitializer(List<Action<IServiceProvider, IMediator>> registrations, IServiceProvider serviceProvider)
    {
        _registrations = registrations;
        _serviceProvider = serviceProvider;
    }

    public void Initialize(IMediator mediator)
    {
    // No need for a scope - we're just registering types, not resolving handlers
        foreach (var registration in _registrations)
{
  registration(_serviceProvider, mediator);
 }
    }
}