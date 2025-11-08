using EcommerceDDD.Domain.Orders;

namespace EcommerceDDD.Infrastructure.Persistence;

/// <summary>
/// Repository en mémoire pour la démonstration
/// En production, cela serait remplacé par une implémentation Entity Framework Core
/// </summary>
public class InMemoryOrderRepository : IOrderRepository
{
 private readonly Dictionary<Guid, Order> _orders = new();
    private readonly object _lock = new();

    public Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
      if (_orders.ContainsKey(order.Id))
        throw new InvalidOperationException($"Order with ID {order.Id} already exists");
  
       _orders[order.Id] = order;
        }
  
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_orders.ContainsKey(order.Id))
     throw new InvalidOperationException($"Order with ID {order.Id} not found");
         
_orders[order.Id] = order;
      }
     
        return Task.CompletedTask;
    }

    public Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
      {
_orders.TryGetValue(id, out var order);
 return Task.FromResult(order);
        }
    }

    public Task<List<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (_lock)
  {
         var orders = _orders.Values
           .Where(o => o.UserId == userId)
 .ToList();
            
         return Task.FromResult(orders);
   }
    }

    public Task<List<Order>> GetAllAsync(CancellationToken cancellationToken = default)
    {
      lock (_lock)
        {
  var orders = _orders.Values.ToList();
   return Task.FromResult(orders);
        }
    }

public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_lock)
   {
            _orders.Remove(id);
   }
 
        return Task.CompletedTask;
    }
}
