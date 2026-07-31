using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace EcommerceDDD.Application.Orders.Commands;

/// <summary>
/// Commande pour confirmer une commande
/// </summary>
public class ConfirmOrderCommand : ICommand
{
    public Guid OrderId { get; set; }
}

/// <summary>
/// Commande pour expédier une commande
/// </summary>
public class ShipOrderCommand : ICommand
{
    public Guid OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
}

/// <summary>
/// Commande pour livrer une commande
/// </summary>
public class DeliverOrderCommand : ICommand
{
    public Guid OrderId { get; set; }
}

/// <summary>
/// Commande pour annuler une commande
/// </summary>
public class CancelOrderCommand : ICommand
{
    public Guid OrderId { get; set; }
}
