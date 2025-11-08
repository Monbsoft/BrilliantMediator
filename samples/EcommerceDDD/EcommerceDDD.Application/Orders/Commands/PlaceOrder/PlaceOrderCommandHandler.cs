using EcommerceDDD.Application.Orders.Dtos;
using EcommerceDDD.Domain.Orders;
using EcommerceDDD.Domain.Orders.Services;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Events;

namespace EcommerceDDD.Application.Orders.Commands.PlaceOrder;

public class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderDomainService _orderDomainService;
    private readonly IMediator _mediator;

    public PlaceOrderCommandHandler(
        IOrderRepository orderRepository,
        IOrderDomainService orderDomainService,
        IMediator mediator)
    {
        _orderRepository = orderRepository;
        _orderDomainService = orderDomainService;
        _mediator = mediator;
    }

    public async Task<PlaceOrderResult> Handle(PlaceOrderCommand command)
    {
        // Convertir les DTOs en value objects
        var orderItems = command.Items
            .Select(item => OrderItem.Create(
                item.ProductId,
                item.ProductName,
                item.Quantity,
                item.Price))
            .ToList();

        // Utiliser le domain service pour créer la commande avec les règles métier
        var order = await _orderDomainService.CreateOrderAsync(command.UserId, orderItems);

        // Persister la commande
        await _orderRepository.AddAsync(order);

        // Publier l'événement
        await _mediator.PublishAsync(new OrderPlacedEvent
        {
            OrderId = order.Id,
            UserId = command.UserId,
            TotalAmount = order.TotalAmount
        });

        return new PlaceOrderResult
        {
            OrderId = order.Id,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString()
        };
    }
}

/// <summary>
/// Événement de domaine - la commande a été placée
/// </summary>
public class OrderPlacedEvent : IEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
}
