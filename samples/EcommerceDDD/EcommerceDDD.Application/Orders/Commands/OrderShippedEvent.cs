using Monbsoft.BrilliantMediator.Abstractions.Events;

namespace EcommerceDDD.Application.Orders.Commands;

public class OrderShippedEvent : IEvent
{
    public Guid OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
}
