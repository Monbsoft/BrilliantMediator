namespace EcommerceDDD.Api.Requests;

public class CreateOrderRequest
{
    public Guid UserId { get; set; }
    public List<OrderItemRequest> Items { get; set; } = new();
}

public class OrderItemRequest
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class UpdateOrderStatusRequest
{
    public string Action { get; set; } = string.Empty; // "confirm", "ship", "deliver", "cancel"
    public string? TrackingNumber { get; set; } // optional for ship action
}
