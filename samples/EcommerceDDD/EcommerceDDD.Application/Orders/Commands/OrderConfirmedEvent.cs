using Monbsoft.BrilliantMediator.Abstractions.Events;

namespace EcommerceDDD.Application.Orders.Commands;

// Domain Events
public class OrderConfirmedEvent : IEvent
{
    public Guid OrderId { get; set; }
}
