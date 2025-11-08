namespace EcommerceDDD.Domain.Orders.Exceptions;

public class OrderNotFoundException : Exception
{
    public OrderNotFoundException(Guid orderId) 
        : base($"Commande avec l'ID {orderId} n'a pas été trouvée") { }
}

public class InvalidOrderStatusException : Exception
{
    public InvalidOrderStatusException(string message) 
        : base(message) { }
}

public class OrderAlreadyExistsException : Exception
{
    public OrderAlreadyExistsException(Guid orderId) 
        : base($"Commande avec l'ID {orderId} existe déjà") { }
}

public class UserCannotPlaceOrderException : Exception
{
  public UserCannotPlaceOrderException(Guid userId) 
   : base($"L'utilisateur {userId} n'est pas autorisé à placer une commande") { }
}

public class InsufficientStockException : Exception
{
    public InsufficientStockException(string productId) 
        : base($"Stock insuffisant pour le produit {productId}") { }
}

public class PaymentFailedException : Exception
{
    public PaymentFailedException() 
        : base("Le paiement a échoué") { }
}
