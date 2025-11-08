using EcommerceDDD.Application.Orders.Dtos;
using EcommerceDDD.Domain.Orders;
using EcommerceDDD.Domain.Orders.Exceptions;
using Monbsoft.BrilliantMediator.Abstractions.Handlers;


namespace EcommerceDDD.Application.Orders.Queries;

public class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
 {
   _orderRepository = orderRepository;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery query)
    {
    var order = await _orderRepository.GetByIdAsync(query.OrderId)
            ?? throw new OrderNotFoundException(query.OrderId);

        return MapToDto(order);
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

public class GetUserOrdersQueryHandler : IQueryHandler<GetUserOrdersQuery, List<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

 public GetUserOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<List<OrderDto>> Handle(GetUserOrdersQuery query)
 {
        var orders = await _orderRepository.GetByUserIdAsync(query.UserId);
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
