using EcommerceDDD.Application.Orders.Dtos;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace EcommerceDDD.Application.Orders.Queries;

/// <summary>
/// Query pour récupérer une commande par ID
/// </summary>
public class GetOrderByIdQuery : IQuery<OrderDto>
{
    public Guid OrderId { get; set; }
}

/// <summary>
/// Query pour récupérer toutes les commandes d'un utilisateur
/// </summary>
public class GetUserOrdersQuery : IQuery<List<OrderDto>>
{
    public Guid UserId { get; set; }
}

/// <summary>
/// Query pour récupérer toutes les commandes
/// </summary>
public class GetAllOrdersQuery : IQuery<List<OrderDto>>
{
}

/// <summary>
/// Query pour récupérer les commandes en attente de confirmation
/// </summary>
public class GetPendingOrdersQuery : IQuery<List<OrderDto>>
{
}
