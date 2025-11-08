using EcommerceDDD.Domain.Orders;
using EcommerceDDD.Domain.Orders.Exceptions;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace EcommerceDDD.Application.Orders.Commands;

public class DeliverOrderCommandHandler : ICommandHandler<DeliverOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;

    public DeliverOrderCommandHandler(IOrderRepository orderRepository, IMediator mediator)
    {
        _orderRepository = orderRepository;
   _mediator = mediator;
    }

public async Task Handle(DeliverOrderCommand command)
    {
        var order = await _orderRepository.GetByIdAsync(command.OrderId)
         ?? throw new OrderNotFoundException(command.OrderId);

        order.Deliver();
 await _orderRepository.UpdateAsync(order);

    await _mediator.PublishAsync(new OrderDeliveredEvent { OrderId = order.Id });
    }
}
