using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using System.Reflection;

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

    /// <summary>
    /// Automatically discovers and registers all handlers in the specified assembly.
    /// Supports ICommandHandler, ICommandHandler&lt;TResponse&gt;, IQueryHandler, and IEventHandler implementations.
    /// </summary>
    /// <param name="assembly">The assembly to scan for handlers.</param>
    /// <param name="lifetime">The service lifetime for registered handlers.</param>
    /// <returns>This builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if assembly is null.</exception>
    public MediatorBuilder AddHandlersFromAssembly(Assembly assembly, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (assembly == null)
            throw new ArgumentNullException(nameof(assembly));

        var types = assembly.GetTypes();

        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsInterface)
                continue;

            var interfaces = type.GetInterfaces();

            foreach (var interfaceType in interfaces)
            {
                if (!interfaceType.IsGenericType)
                    continue;

                var genericDefinition = interfaceType.GetGenericTypeDefinition();

                // Handle ICommandHandler<TCommand>
                if (genericDefinition == typeof(ICommandHandler<>))
                {
                    var commandType = interfaceType.GetGenericArguments()[0];
                    RegisterCommandHandlerType(type, interfaceType, commandType, lifetime);
                }
                // Handle ICommandHandler<TCommand, TResponse>
                else if (genericDefinition == typeof(ICommandHandler<,>))
                {
                    var args = interfaceType.GetGenericArguments();
                    var commandType = args[0];
                    var responseType = args[1];
                    RegisterCommandHandlerWithResponseType(type, interfaceType, commandType, responseType, lifetime);
                }
                // Handle IQueryHandler<TQuery, TResponse>
                else if (genericDefinition == typeof(IQueryHandler<,>))
                {
                    var args = interfaceType.GetGenericArguments();
                    var queryType = args[0];
                    var responseType = args[1];
                    RegisterQueryHandlerType(type, interfaceType, queryType, responseType, lifetime);
                }
                // Handle IEventHandler<TEvent>
                else if (genericDefinition == typeof(IEventHandler<>))
                {
                    var eventType = interfaceType.GetGenericArguments()[0];
                    RegisterEventHandlerType(type, interfaceType, eventType, lifetime);
                }
            }
        }

        return this;
    }

    /// <summary>
    /// Automatically discovers and registers all handlers from multiple assemblies.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan for handlers.</param>
    /// <param name="lifetime">The service lifetime for registered handlers.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public MediatorBuilder AddHandlersFromAssemblies(IEnumerable<Assembly> assemblies, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (assemblies == null)
            throw new ArgumentNullException(nameof(assemblies));

        foreach (var assembly in assemblies)
        {
            AddHandlersFromAssembly(assembly, lifetime);
        }

        return this;
    }

    /// <summary>
    /// Automatically discovers and registers all handlers from assemblies matching the given names.
    /// </summary>
    /// <param name="assemblyNames">The names of the assemblies to load and scan.</param>
    /// <param name="lifetime">The service lifetime for registered handlers.</param>
    /// <returns>This builder instance for method chaining.</returns>
    public MediatorBuilder AddHandlersFromAssemblyNames(IEnumerable<string> assemblyNames, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        if (assemblyNames == null)
            throw new ArgumentNullException(nameof(assemblyNames));

        var assemblies = assemblyNames
     .Select(name => Assembly.Load(name))
      .ToList();

        return AddHandlersFromAssemblies(assemblies, lifetime);
    }

    private void RegisterCommandHandlerType(Type handlerType, Type interfaceType, Type commandType, ServiceLifetime lifetime)
    {
        _services.Add(new ServiceDescriptor(interfaceType, handlerType, lifetime));

        var registerMethod = typeof(MediatorBuilder)
    .GetMethod(nameof(RegisterCommandHandlerGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
       .MakeGenericMethod(commandType);

        _handlerRegistrations.Add((provider, mediator) =>
       {
           registerMethod.Invoke(null, new object[] { provider, mediator, handlerType });
       });
    }

    private void RegisterCommandHandlerWithResponseType(Type handlerType, Type interfaceType, Type commandType, Type responseType, ServiceLifetime lifetime)
    {
        _services.Add(new ServiceDescriptor(interfaceType, handlerType, lifetime));

        var registerMethod = typeof(MediatorBuilder)
.GetMethod(nameof(RegisterCommandHandlerGenericWithResponse), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(commandType, responseType);

        _handlerRegistrations.Add((provider, mediator) =>
             {
                 registerMethod.Invoke(null, new object[] { provider, mediator, handlerType });
             });
    }

    private void RegisterQueryHandlerType(Type handlerType, Type interfaceType, Type queryType, Type responseType, ServiceLifetime lifetime)
    {
        _services.Add(new ServiceDescriptor(interfaceType, handlerType, lifetime));

        var registerMethod = typeof(MediatorBuilder)
            .GetMethod(nameof(RegisterQueryHandlerGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
     .MakeGenericMethod(queryType, responseType);

        _handlerRegistrations.Add((provider, mediator) =>
        {
            registerMethod.Invoke(null, new object[] { provider, mediator, handlerType });
        });
    }

    private void RegisterEventHandlerType(Type handlerType, Type interfaceType, Type eventType, ServiceLifetime lifetime)
    {
        _services.Add(new ServiceDescriptor(interfaceType, handlerType, lifetime));

        var registerMethod = typeof(MediatorBuilder)
            .GetMethod(nameof(RegisterEventHandlerGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(eventType);

        _handlerRegistrations.Add((provider, mediator) =>
        {
            registerMethod.Invoke(null, new object[] { provider, mediator, handlerType });
        });
    }

    private static void RegisterCommandHandlerGeneric<TCommand>(IServiceProvider provider, IMediator mediator, Type handlerType)
        where TCommand : ICommand
  {
   mediator.RegisterCommandHandler<TCommand>();
    }

    private static void RegisterCommandHandlerGenericWithResponse<TCommand, TResponse>(IServiceProvider provider, IMediator mediator, Type handlerType)
  where TCommand : ICommand<TResponse>
    {
   mediator.RegisterCommandHandler<TCommand, TResponse>();
    }

    private static void RegisterQueryHandlerGeneric<TQuery, TResponse>(IServiceProvider provider, IMediator mediator, Type handlerType)
        where TQuery : IQuery<TResponse>
    {
 mediator.RegisterQueryHandler<TQuery, TResponse>();
    }

    private static void RegisterEventHandlerGeneric<TEvent>(IServiceProvider provider, IMediator mediator, Type handlerType)
        where TEvent : IEvent
    {
        mediator.RegisterEventHandler<TEvent>();
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