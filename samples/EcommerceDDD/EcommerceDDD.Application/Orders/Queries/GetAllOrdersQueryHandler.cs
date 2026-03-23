using EcommerceDDD.Application.Orders.Dtos;
using EcommerceDDD.Domain.Orders;
using Monbsoft.BrilliantMediator.Abstractions.Queries;


namespace EcommerceDDD.Application.Orders.Queries;

public class GetAllOrdersQueryHandler : IQueryHandler<GetAllOrdersQuery, List<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetAllOrdersQueryHandler(IOrderRepository orderRepository)
{
        _orderRepository = orderRepository;
    }

    public async Task<List<OrderDto>> Handle(GetAllOrdersQuery query, CancellationToken cancellationToken = default)
    {
      var orders = await _orderRepository.GetAllAsync();
        return orders.Select(MapToDto).ToList();
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
