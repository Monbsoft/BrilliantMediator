using EcommerceDDD.Application.Orders.Dtos;
using EcommerceDDD.Domain.Orders;
using Monbsoft.BrilliantMediator.Abstractions.Queries;


namespace EcommerceDDD.Application.Orders.Queries;

public class GetPendingOrdersQueryHandler : IQueryHandler<GetPendingOrdersQuery, List<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetPendingOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

  public async Task<List<OrderDto>> Handle(GetPendingOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var allOrders = await _orderRepository.GetAllAsync();
     var pendingOrders = allOrders.Where(o => o.Status == OrderStatus.Pending).ToList();
        return pendingOrders.Select(MapToDto).ToList();
 }

    private static OrderDto MapToDto(Order order)
 {
 return new OrderDto
      {
    Id = order.Id,
     UserId = order.UserId,
     Items = order.Items.Select(item => new OrderItemDto
      {
    ProductId = item.ProductId,
      ProductName = item.ProductName,
             Quantity = item.Quantity,
  Price = item.Price,
    Total = item.Total
      }).ToList(),
         TotalAmount = order.TotalAmount,
       Status = (int)order.Status,
  StatusName = order.Status.ToString(),
     CreatedAt = order.CreatedAt,
  ConfirmedAt = order.ConfirmedAt,
      ShippedAt = order.ShippedAt,
    DeliveredAt = order.DeliveredAt
      };
    }
}
