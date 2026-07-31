using EcommerceDDD.Application.Orders.Dtos;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace EcommerceDDD.Application.Orders.Commands.PlaceOrder;

/// <summary>
/// Commande pour placer une nouvelle commande
/// </summary>
public class PlaceOrderCommand : ICommand<PlaceOrderResult>
{
    public Guid UserId { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

public class PlaceOrderResult
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}
