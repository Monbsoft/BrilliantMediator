using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monbsoft.BrilliantMediator.Extensions;

/// <summary>
/// Builder for fluent registration of handlers.
/// Zero reflection - all at compile-time with generics.
/// </summary>
public sealed class MediatorBuilder
{
    private readonly IServiceCollection _services;

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

        // Auto-wire into mediator
        _services.AddSingleton(provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            var handler = provider.GetRequiredService<ICommandHandler<TCommand>>();
            mediator.RegisterCommandHandler(handler);
            return mediator;
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

        // Auto-wire into mediator
        _services.AddSingleton(provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            var handler = provider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
            mediator.RegisterCommandHandler(handler);
            return mediator;
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

        // Auto-wire into mediator
        _services.AddSingleton(provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            var handler = provider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
            mediator.RegisterQueryHandler(handler);
            return mediator;
        });

        return this;
    }

    /// <summary>
    /// Registers a command handler instance directly.
    /// Zero reflection, no DI lookup.
    /// </summary>
    public MediatorBuilder AddCommandHandlerInstance<TCommand>(
        ICommandHandler<TCommand> handler)
        where TCommand : ICommand
    {
        _services.AddSingleton(provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            mediator.RegisterCommandHandler(handler);
            return mediator;
        });

        return this;
    }

    /// <summary>
    /// Registers a command handler instance directly with response.
    /// Zero reflection, no DI lookup.
    /// </summary>
    public MediatorBuilder AddCommandHandlerInstance<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> handler)
        where TCommand : ICommand<TResponse>
    {
        _services.AddSingleton(provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            mediator.RegisterCommandHandler(handler);
            return mediator;
        });

        return this;
    }

    /// <summary>
    /// Registers a query handler instance directly.
    /// Zero reflection, no DI lookup.
    /// </summary>
    public MediatorBuilder AddQueryHandlerInstance<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> handler)
        where TQuery : IQuery<TResponse>
    {
        _services.AddSingleton(provider =>
        {
            var mediator = provider.GetRequiredService<IMediator>();
            mediator.RegisterQueryHandler(handler);
            return mediator;
        });

        return this;
    }

    /// <summary>
    /// Finalizes the builder and returns the service collection.
    /// </summary>
    public IServiceCollection Build()
    {
        return _services;
    }
}
