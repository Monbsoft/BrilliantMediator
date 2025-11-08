# 🏗️ Ecommerce DDD - Exemple complet avec BrilliantMediator

Ce projet montre comment implémenter une architecture **Domain-Driven Design (DDD)** avec **BrilliantMediator** et une **WebAPI**.

## 📋 Structure du projet

```
EcommerceDDD/
├── EcommerceDDD.Domain/       # Couche Domaine
│   └── Orders/
│       ├── Order.cs         # Agrégat racine
│       ├── OrderItem.cs              # Value Object
│       ├── OrderStatus.cs            # Énumération
│       ├── IOrderRepository.cs        # Interface du repository
│       ├── Services/
│       │   └── OrderDomainService.cs # Service de domaine
│       └── Exceptions/
│           └── OrderExceptions.cs    # Exceptions métier
│
├── EcommerceDDD.Application/         # Couche Application
│└── Orders/
│       ├── Commands/
│       │   ├── PlaceOrder/
│       │   │ ├── PlaceOrderCommand.cs
│       │   │   └── PlaceOrderCommandHandler.cs
│       │   └── ManageOrder/
│       │       ├── ConfirmOrderCommand.cs
│       │       ├── ShipOrderCommand.cs
│    │  ├── DeliverOrderCommand.cs
│   │       ├── CancelOrderCommand.cs
│       │       └── *CommandHandlers.cs
│       ├── Queries/
││   ├── OrderQueries.cs
│     │   └── OrderQueryHandlers.cs
│       ├── EventHandlers/
│  │   └── OrderEventHandlers.cs
│       └── Dtos/
│           └── OrderDtos.cs
│
├── EcommerceDDD.Infrastructure/      # Couche Infrastructure
│   ├── Persistence/
│   │   └── InMemoryOrderRepository.cs
│   └── DependencyInjection/
│     └── ServiceCollectionExtensions.cs
│
└── EcommerceDDD.Api/        # API REST
    ├── Controllers/
  │   └── OrdersController.cs
    ├── Requests/
    │   └── OrderRequests.cs
    ├── Program.cs
    ├── appsettings.json
    └── Properties/
        └── launchSettings.json
```

## 🎯 Concepts DDD implémentés

### 1. **Agrégat Racine** (Order)
- Entité qui encapsule la logique métier
- Assure l'intégrité des données via des factory methods et des méthodes d'action
- Valide les transitions d'état

```csharp
public static Order Create(Guid userId, List<OrderItem> items)
{
    if (items == null || items.Count == 0)
throw new InvalidOperationException("Une commande doit contenir au moins un article");
    
    return new Order { /* ... */ };
}

public void Confirm()
{
    if (Status != OrderStatus.Pending)
        throw new InvalidOperationException("Seules les commandes en attente peuvent être confirmées");
    
    Status = OrderStatus.Confirmed;
}
```

### 2. **Value Objects** (OrderItem)
- Immuables et sans identité propre
- Comparables par valeur
- Encapsulent la logique métier locale

```csharp
public class OrderItem
{
    public static OrderItem Create(string productId, string productName, 
        int quantity, decimal price)
    {
        // Validations métier
        if (quantity <= 0)
        throw new ArgumentException("Quantity must be greater than 0");
        
        return new OrderItem { /* ... */ };
    }
}
```

### 3. **Repository** (IOrderRepository)
- Abstraction pour la persistance
- Une interface par agrégat
- Accessible uniquement via l'agrégat racine

```csharp
public interface IOrderRepository
{
    Task AddAsync(Order order);
    Task UpdateAsync(Order order);
    Task<Order?> GetByIdAsync(Guid id);
    // ...
}
```

### 4. **Domain Services** (OrderDomainService)
- Contiennent la logique métier complexe
- Opèrent sur plusieurs agrégats si nécessaire
- Pas d'état

```csharp
public class OrderDomainService : IOrderDomainService
{
    public async Task<Order> CreateOrderAsync(Guid userId, List<OrderItem> items)
    {
        var canPlace = await CanUserPlaceOrderAsync(userId);
        if (!canPlace)
   throw new InvalidOperationException("Non autorisé");
        
        return Order.Create(userId, items);
    }
}
```

### 5. **Domain Events**
- Représentent des événements importants du domaine
- Déclenchent des actions via les event handlers
- Assurent la communication entre agrégats

```csharp
public class OrderPlacedEvent : IEvent
{
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal TotalAmount { get; set; }
}
```

## 🔄 Flux de traitement

### Placer une commande

```
Client HTTP
    ↓
POST /api/orders
    ↓
OrdersController.PlaceOrder()
    ↓
PlaceOrderCommand
    ↓
PlaceOrderCommandHandler
    ├─ OrderDomainService.CreateOrderAsync()
    │   └─ Valide les règles métier
    ├─ IOrderRepository.AddAsync()
    │   └─ Persiste la commande
    ├─ Mediator.Publish(OrderPlacedEvent)
    │   └─ Déclenche les event handlers
  └─ Retourne PlaceOrderResult
        ↓
        Response HTTP 201
```

### Récupérer une commande

```
Client HTTP
    ↓
GET /api/orders/{id}
    ↓
OrdersController.GetOrderById()
    ↓
GetOrderByIdQuery
    ↓
GetOrderByIdQueryHandler
    ├─ IOrderRepository.GetByIdAsync()
    ├─ MapToDto()
    └─ Retourne OrderDto
        ↓
    Response HTTP 200
```

## 🚀 Utilisation

### Démarrer l'API

```bash
cd samples/EcommerceDDD/EcommerceDDD.Api
dotnet run
```

L'API sera disponible sur:
- **HTTP**: http://localhost:5000
- **HTTPS**: https://localhost:7000
- **Swagger**: http://localhost:5000/swagger

### Exemples de requêtes

#### 1. Placer une commande

```bash
curl -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "items": [
      {
        "productId": "PROD-001",
        "productName": "Laptop",
        "quantity": 1,
        "price": 999.99
      },
      {
        "productId": "PROD-002",
 "productName": "Mouse",
        "quantity": 2,
        "price": 29.99
      }
    ]
  }'
```

Réponse:
```json
{
  "orderId": "550e8400-e29b-41d4-a716-446655440001",
  "totalAmount": 1059.97,
  "status": "Pending"
}
```

#### 2. Récupérer une commande

```bash
curl http://localhost:5000/api/orders/550e8400-e29b-41d4-a716-446655440001
```

#### 3. Confirmer une commande

```bash
curl -X PUT http://localhost:5000/api/orders/550e8400-e29b-41d4-a716-446655440001/status \
  -H "Content-Type: application/json" \
  -d '{
    "action": "confirm"
  }'
```

#### 4. Expédier une commande

```bash
curl -X PUT http://localhost:5000/api/orders/550e8400-e29b-41d4-a716-446655440001/status \
  -H "Content-Type: application/json" \
  -d '{
    "action": "ship",
    "trackingNumber": "TRACK-123456789"
  }'
```

#### 5. Récupérer les commandes d'un utilisateur

```bash
curl http://localhost:5000/api/orders/user/550e8400-e29b-41d4-a716-446655440000
```

#### 6. Récupérer les commandes en attente

```bash
curl http://localhost:5000/api/orders/pending
```

## 🏛️ Architecture en couches

### 1. **Domain Layer** (EcommerceDDD.Domain)
- Contient les entités, value objects et services de domaine
- Aucune dépendance externe
- Logique métier pure

### 2. **Application Layer** (EcommerceDDD.Application)
- Commands, Queries, Event Handlers
- DTOs pour la communication
- Orchestration entre domaine et infrastructure

### 3. **Infrastructure Layer** (EcommerceDDD.Infrastructure)
- Implémentation des repositories
- Configuration de l'injection de dépendances
- Services externes (base de données, emails, etc.)

### 4. **Presentation Layer** (EcommerceDDD.Api)
- Controllers
- HTTP requests/responses
- Swagger documentation

## 📦 Dépendances

```
EcommerceDDD.Api
  ├─ EcommerceDDD.Infrastructure
  │  ├─ EcommerceDDD.Application
  │  │  └─ EcommerceDDD.Domain
  │  └─ BrilliantMediator
  └─ Serilog (logging)
```

## 🔌 Injection de dépendances

L'extension `AddEcommerceDDD()` configure automatiquement:

```csharp
services.AddEcommerceDDD();
```

Cela enregistre:
- ✅ BrilliantMediator
- ✅ IOrderRepository (InMemoryOrderRepository)
- ✅ IOrderDomainService (OrderDomainService)
- ✅ Tous les Commands, Queries et Event Handlers (découverte automatique)

## 📊 Modèle de données

### Order (Agrégat racine)

```
Order
├── Id: Guid
├── UserId: Guid
├── Items: List<OrderItem>
├── TotalAmount: decimal
├── Status: OrderStatus
├── CreatedAt: DateTime
├── ConfirmedAt: DateTime?
├── ShippedAt: DateTime?
└── DeliveredAt: DateTime?
```

### OrderItem (Value Object)

```
OrderItem
├── ProductId: string
├── ProductName: string
├── Quantity: int
├── Price: decimal
└── Total: decimal (calculé)
```

### OrderStatus (Énumération)

```
- Pending (0)
- Confirmed (1)
- Shipped (2)
- Delivered (3)
- Cancelled (4)
```

## 🎓 Bonnes pratiques DDD appliquées

✅ **Langage Ubiquitaire** - Noms explicites (Order, OrderItem, OrderStatus)

✅ **Agrégats** - Order est l'agrégat racine, OrderItem en fait partie

✅ **Value Objects** - OrderItem est immuable et comparable par valeur

✅ **Domain Services** - OrderDomainService pour la logique complexe

✅ **Repositories** - IOrderRepository pour l'abstraction de persistance

✅ **Domain Events** - OrderPlacedEvent pour la communication

✅ **Exceptions métier** - OrderNotFoundException, UserCannotPlaceOrderException

✅ **Validation** - Dans l'agrégat via factory methods et méthodes d'action

## 🔄 Diagramme d'état

```
        Create
   ↓
      Pending ←---→ Cancelled
        ↓
    Confirmed
        ↓
    Shipped
        ↓
    Delivered
```

Les transitions invalides lèvent une `InvalidOperationException`.

## 📝 Logging

L'application utilise **Serilog** pour le logging:

```
📋 Commande placée: {OrderId} pour l'utilisateur {UserId} - Montant: {TotalAmount}€
✅ Commande confirmée: {OrderId}
📦 Commande expédiée: {OrderId} - Tracking: {TrackingNumber}
🎉 Commande livrée: {OrderId}
❌ Commande annulée: {OrderId}
```

Les logs sont écrits en console et en fichier (`logs/ecommerce-{date}.txt`).

## 🚀 Prochaines étapes

Pour une implémentation production:

1. **Remplacer InMemoryOrderRepository** par une implémentation Entity Framework Core
2. **Ajouter des migrations** de base de données
3. **Implémenter Unit of Work** pour la gestion des transactions
4. **Ajouter des spécifications** (ISpecification pattern)
5. **Intégrer CQRS** avec une base de données de lecture séparée
6. **Ajouter des tests** unitaires et d'intégration
7. **Implémenter l'authentification** et l'autorisation
8. **Ajouter la validation** avec FluentValidation
9. **Intégrer une file d'attente** pour les événements (RabbitMQ, Azure Service Bus)
10. **Ajouter des saga** pour les transactions distribuées

## 📚 Ressources

- [Domain-Driven Design - Eric Evans](https://domainlanguage.com/ddd/)
- [BrilliantMediator Documentation](../README.md)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)

## 📄 Licence

MIT License - Voir le fichier LICENSE dans la racine du projet
