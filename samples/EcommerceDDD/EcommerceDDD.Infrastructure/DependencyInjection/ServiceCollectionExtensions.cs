using EcommerceDDD.Domain.Orders;
using EcommerceDDD.Domain.Orders.Services;
using EcommerceDDD.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceDDD.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Enregistre les services d'infrastructure (repositories, domain services).
    /// BrilliantMediator est configuré dans le projet Web via AddGeneratedHandlers().
    /// </summary>
    public static IServiceCollection AddEcommerceDDD(this IServiceCollection services)
    {
        services.AddScoped<IOrderRepository, InMemoryOrderRepository>();
        services.AddScoped<IOrderDomainService, OrderDomainService>();

        return services;
    }
}