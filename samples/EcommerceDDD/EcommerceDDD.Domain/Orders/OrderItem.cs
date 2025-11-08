namespace EcommerceDDD.Domain.Orders;

/// <summary>
/// Status possible d'une commande
/// </summary>
public enum OrderStatus
{
    Pending = 0,
Confirmed = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4
}

/// <summary>
/// Value Object représentant un article dans une commande
/// </summary>
public class OrderItem
{
    public string ProductId { get; private set; }
    public string ProductName { get; private set; }
    public int Quantity { get; private set; }
    public decimal Price { get; private set; }
    public decimal Total => Price * Quantity;

 private OrderItem() { }

    /// <summary>
    /// Factory method pour créer un nouvel item de commande
    /// </summary>
    public static OrderItem Create(string productId, string productName, int quantity, decimal price)
    {
        if (string.IsNullOrWhiteSpace(productId))
         throw new ArgumentException("Product ID cannot be null or empty", nameof(productId));

     if (string.IsNullOrWhiteSpace(productName))
         throw new ArgumentException("Product name cannot be null or empty", nameof(productName));

    if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (price <= 0)
      throw new ArgumentException("Price must be greater than 0", nameof(price));

    return new OrderItem
        {
    ProductId = productId,
           ProductName = productName,
        Quantity = quantity,
 Price = price
   };
    }

    public override bool Equals(object? obj)
    {
 if (obj is not OrderItem other)
   return false;

return ProductId == other.ProductId &&
    Quantity == other.Quantity &&
         Price == other.Price;
    }

    public override int GetHashCode()
    {
   return HashCode.Combine(ProductId, Quantity, Price);
    }
}
