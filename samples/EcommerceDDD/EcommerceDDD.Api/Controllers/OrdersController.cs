using EcommerceDDD.Api.Requests;
using EcommerceDDD.Application.Orders.Commands;
using EcommerceDDD.Application.Orders.Commands.PlaceOrder;
using EcommerceDDD.Application.Orders.Dtos;
using EcommerceDDD.Application.Orders.Queries;
using Microsoft.AspNetCore.Mvc;
using Monbsoft.BrilliantMediator.Abstractions;

namespace EcommerceDDD.Api.Controllers;

/// <summary>
/// API pour gérer les commandes e-commerce
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IMediator mediator, ILogger<OrdersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Placer une nouvelle commande
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PlaceOrder([FromBody] CreateOrderRequest request)
    {
        try
        {
            _logger.LogInformation($"Placement d'une commande pour l'utilisateur {request.UserId}");

            var command = new PlaceOrderCommand
            {
                UserId = request.UserId,
                Items = request.Items.Select(item => new CreateOrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Price = item.Price
                }).ToList()
            };

            var result = await _mediator.DispatchAsync<PlaceOrderCommand, PlaceOrderResult>(command);

            return CreatedAtAction(nameof(GetOrderById), new { id = result.OrderId }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors du placement de la commande");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Récupérer une commande par ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id)
    {
        try
        {
            var query = new GetOrderByIdQuery { OrderId = id };
            var result = await _mediator.SendAsync<GetOrderByIdQuery, OrderDto>(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la récupération de la commande {id}");
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Récupérer toutes les commandes d'un utilisateur
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserOrders(Guid userId)
    {
        try
        {
            var query = new GetUserOrdersQuery { UserId = userId };
            var result = await _mediator.SendAsync<GetUserOrdersQuery, List<OrderDto>>(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la récupération des commandes de l'utilisateur {userId}");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Récupérer toutes les commandes
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders()
    {
        try
        {
            var query = new GetAllOrdersQuery();
            var result = await _mediator.SendAsync<GetAllOrdersQuery, List<OrderDto>>(query);
            await Task.Delay(2);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération de toutes les commandes");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Récupérer les commandes en attente
    /// </summary>
    [HttpGet("pending")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingOrders()
    {
        try
        {
            var query = new GetPendingOrdersQuery();
            var result = await _mediator.SendAsync<GetPendingOrdersQuery, List<OrderDto>>(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des commandes en attente");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Mettre à jour le statut d'une commande
    /// </summary>
    [HttpPut("{id}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        try
        {
            _logger.LogInformation($"Mise à jour du statut de la commande {id} - Action: {request.Action}");

            switch (request.Action.ToLower())
            {
                case "confirm":
                    await _mediator.DispatchAsync(new ConfirmOrderCommand { OrderId = id });
                    break;

                case "ship":
                    await _mediator.DispatchAsync(new ShipOrderCommand
                    {
                        OrderId = id,
                        TrackingNumber = request.TrackingNumber ?? "TRK-" + Guid.NewGuid().ToString()[..8]
                    });
                    break;

                case "deliver":
                    await _mediator.DispatchAsync(new DeliverOrderCommand { OrderId = id });
                    break;

                case "cancel":
                    await _mediator.DispatchAsync(new CancelOrderCommand { OrderId = id });
                    break;

                default:
                    return BadRequest(new { error = "Action invalide. Utilisez: confirm, ship, deliver, cancel" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erreur lors de la mise à jour du statut de la commande {id}");
            return BadRequest(new { error = ex.Message });
        }
    }
}