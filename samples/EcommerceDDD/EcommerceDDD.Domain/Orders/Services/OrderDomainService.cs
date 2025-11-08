namespace EcommerceDDD.Domain.Orders.Services;

/// <summary>
/// Service de domaine pour les règles métier complexes de commande
/// </summary>
public interface IOrderDomainService
{
    Task<Order> CreateOrderAsync(Guid userId, List<OrderItem> items, CancellationToken cancellationToken = default);
    Task<bool> CanUserPlaceOrderAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class OrderDomainService : IOrderDomainService
{
    private readonly IOrderRepository _orderRepository;

    public OrderDomainService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<Order> CreateOrderAsync(Guid userId, List<OrderItem> items, CancellationToken cancellationToken = default)
    {
    // Vérifier si l'utilisateur peut placer une commande
        var canPlace = await CanUserPlaceOrderAsync(userId, cancellationToken);
        
        if (!canPlace)
            throw new InvalidOperationException("Cet utilisateur n'est pas autorisé à placer une commande");

        // Créer la commande
        var order = Order.Create(userId, items);
 
        return order;
    }

    public async Task<bool> CanUserPlaceOrderAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Récupérer les commandes récentes de l'utilisateur
   var recentOrders = await _orderRepository.GetByUserIdAsync(userId, cancellationToken);
  
        // Exemple: limiter à 5 commandes par jour
        var ordersToday = recentOrders
            .Where(o => o.CreatedAt.Date == DateTime.UtcNow.Date)
       .Count();

        return ordersToday < 5;
    }
}
