using EcommerceDDD.Domain.Orders;
using EcommerceDDD.Domain.Orders.Exceptions;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace EcommerceDDD.Application.Orders.Commands;

public class CancelOrderCommandHandler : ICommandHandler<CancelOrderCommand>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IMediator _mediator;

    public CancelOrderCommandHandler(IOrderRepository orderRepository, IMediator mediator)
    {
   _orderRepository = orderRepository;
        _mediator = mediator;
    }

    public async Task Handle(CancelOrderCommand command, CancellationToken cancellationToken = default)
    {
 var order = await _orderRepository.GetByIdAsync(command.OrderId)
      ?? throw new OrderNotFoundException(command.OrderId);

        order.Cancel();
        await _orderRepository.UpdateAsync(order);

        await _mediator.PublishAsync(new OrderCancelledEvent { OrderId = order.Id });
    }
}
