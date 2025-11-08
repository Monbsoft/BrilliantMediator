using Monbsoft.BrilliantMediator.Abstractions.Events;

namespace EcommerceDDD.Application.Orders.Commands;

public class OrderDeliveredEvent : IEvent
{
    public Guid OrderId { get; set; }
}
