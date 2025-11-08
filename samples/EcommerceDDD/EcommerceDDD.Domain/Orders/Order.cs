namespace EcommerceDDD.Domain.Orders;

/// <summary>
/// Entité racine d'agrégat pour une commande
/// </summary>
public class Order
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public List<OrderItem> Items { get; private set; } = new();
 public decimal TotalAmount { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? ShippedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }

    private Order() { }

    /// <summary>
  /// Factory method pour créer une nouvelle commande
    /// </summary>
    public static Order Create(Guid userId, List<OrderItem> items)
    {
      if (items == null || items.Count == 0)
            throw new InvalidOperationException("Une commande doit contenir au moins un article");

        return new Order
        {
    Id = Guid.NewGuid(),
     UserId = userId,
       Items = items,
   TotalAmount = items.Sum(i => i.Total),
     Status = OrderStatus.Pending,
       CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Confirme la commande
    /// </summary>
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
     throw new InvalidOperationException("Seules les commandes en attente peuvent être confirmées");

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marque la commande comme expédiée
    /// </summary>
    public void Ship()
    {
        if (Status != OrderStatus.Confirmed)
      throw new InvalidOperationException("Seules les commandes confirmées peuvent être expédiées");

        Status = OrderStatus.Shipped;
   ShippedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marque la commande comme livrée
    /// </summary>
    public void Deliver()
    {
        if (Status != OrderStatus.Shipped)
            throw new InvalidOperationException("Seules les commandes expédiées peuvent être livrées");

        Status = OrderStatus.Delivered;
 DeliveredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Annule la commande
    /// </summary>
    public void Cancel()
    {
        if (Status is OrderStatus.Shipped or OrderStatus.Delivered)
            throw new InvalidOperationException("Les commandes expédiées ou livrées ne peuvent pas être annulées");

        Status = OrderStatus.Cancelled;
    }
}
