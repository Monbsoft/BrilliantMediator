using EcommerceDDD.Application.Orders.Commands;
using EcommerceDDD.Application.Orders.Commands.PlaceOrder;
using EcommerceDDD.Application.Orders.Dtos;
using EcommerceDDD.Application.Orders.EventHandlers;
using EcommerceDDD.Application.Orders.Queries;
using EcommerceDDD.Domain.Orders;
using EcommerceDDD.Domain.Orders.Services;
using EcommerceDDD.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Extensions;

namespace EcommerceDDD.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre tous les services e-commerce avec BrilliantMediator
    /// </summary>
    public static IServiceCollection AddEcommerceDDD(this IServiceCollection services)
    {
        // Enregistrer le repository avec un lifetime Scoped (compatible avec les handlers)
        services.AddScoped<IOrderRepository, InMemoryOrderRepository>();

        // Enregistrer les domain services
        services.AddScoped<IOrderDomainService, OrderDomainService>();
        var appAssembly = typeof(GetAllOrdersQueryHandler).Assembly;

        // Ajouter BrilliantMediator avec découverte automatique des handlers
        services.AddBrilliantMediator(appAssembly, lifetime: ServiceLifetime.Scoped);

        return services;
    }

    /// <summary>
    /// Configuration alternative - enregistrement manuel des handlers
    /// </summary>
    public static IServiceCollection AddEcommerceDDDManual(
        this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
        services.AddScoped<IOrderDomainService, OrderDomainService>();

        services.AddBrilliantMediator()
        // Commands
            .AddCommandHandler<PlaceOrderCommand, PlaceOrderResult, PlaceOrderCommandHandler>()
            .AddCommandHandler<ConfirmOrderCommand, ConfirmOrderCommandHandler>()
            .AddCommandHandler<ShipOrderCommand, ShipOrderCommandHandler>()
            .AddCommandHandler<DeliverOrderCommand, DeliverOrderCommandHandler>()
            .AddCommandHandler<CancelOrderCommand, CancelOrderCommandHandler>()

        // Queries
        .AddQueryHandler<GetOrderByIdQuery, OrderDto, GetOrderByIdQueryHandler>()
        .AddQueryHandler<GetUserOrdersQuery, List<OrderDto>, GetUserOrdersQueryHandler>()
        .AddQueryHandler<GetAllOrdersQuery, List<OrderDto>, GetAllOrdersQueryHandler>()
        .AddQueryHandler<GetPendingOrdersQuery, List<OrderDto>, GetPendingOrdersQueryHandler>()

        // Event Handlers
        .AddEventHandler<OrderPlacedEvent, OrderPlacedEventHandler>()
        .Build();

        return services;
    }
}