using EcommerceDDD.Application.Orders.Commands;
using EcommerceDDD.Application.Orders.Commands.PlaceOrder;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Events;

namespace EcommerceDDD.Application.Orders.EventHandlers;

public class OrderPlacedEventHandler : IEventHandler<OrderPlacedEvent>
{
    private readonly ILogger<OrderPlacedEventHandler> _logger;

    public OrderPlacedEventHandler(ILogger<OrderPlacedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderPlacedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"📋 Commande placée: {@event.OrderId} pour l'utilisateur {@event.UserId} - Montant: {@event.TotalAmount}€");
        await Task.CompletedTask;
    }
}

public class OrderConfirmedEventHandler : IEventHandler<OrderConfirmedEvent>
{
    private readonly ILogger<OrderConfirmedEventHandler> _logger;

    public OrderConfirmedEventHandler(ILogger<OrderConfirmedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderConfirmedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"✅ Commande confirmée: {@event.OrderId}");
        await Task.CompletedTask;
    }
}

public class OrderShippedEventHandler : IEventHandler<OrderShippedEvent>
{
    private readonly ILogger<OrderShippedEventHandler> _logger;

    public OrderShippedEventHandler(ILogger<OrderShippedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderShippedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"📦 Commande expédiée: {@event.OrderId} - Tracking: {@event.TrackingNumber}");
        await Task.CompletedTask;
    }
}

public class OrderDeliveredEventHandler : IEventHandler<OrderDeliveredEvent>
{
    private readonly ILogger<OrderDeliveredEventHandler> _logger;

    public OrderDeliveredEventHandler(ILogger<OrderDeliveredEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderDeliveredEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"🎉 Commande livrée: {@event.OrderId}");
        await Task.CompletedTask;
    }
}

public class OrderCancelledEventHandler : IEventHandler<OrderCancelledEvent>
{
    private readonly ILogger<OrderCancelledEventHandler> _logger;

    public OrderCancelledEventHandler(ILogger<OrderCancelledEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(OrderCancelledEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning($"❌ Commande annulée: {@event.OrderId}");
        await Task.CompletedTask;
    }
}