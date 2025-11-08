using Monbsoft.BrilliantMediator.Abstractions.Events;

namespace EcommerceDDD.Application.Orders.Commands;

public class OrderCancelledEvent : IEvent
{
    public Guid OrderId { get; set; }
}