# BrilliantMediator - Guide Simple

## Vue d'ensemble

BrilliantMediator est un médiateur ultra-léger et ultra-rapide pour .NET qui implémente le pattern Mediator sans réflexion.

## Installation

```bash
dotnet add package BrilliantMediator
```

## Configuration rapide

### Avec Microsoft.Extensions.DependencyInjection

```csharp
using Microsoft.Extensions.DependencyInjection;
using BrilliantMediator.DependencyInjection;

var services = new ServiceCollection();
services.AddBrilliantMediator(typeof(Program).Assembly);

var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<Mediator>();
```

## Les trois concepts clés

### 1. Commands (Commandes)

Les commandes représentent des **actions qui modifient l'état**.

#### Sans réponse

```csharp
public class SendEmailCommand : ICommand
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string Body { get; set; }
}

public class SendEmailCommandHandler : ICommandHandler<SendEmailCommand>
{
    public async Task Handle(SendEmailCommand command)
    {
        // Envoyer l'email
        await _emailService.SendAsync(command.To, command.Subject, command.Body);
    }
}

// Utilisation
await mediator.Send(new SendEmailCommand 
{ 
    To = "user@example.com", 
    Subject = "Bienvenue",
    Body = "Bienvenue sur notre plateforme!"
});
```

#### Avec réponse

```csharp
public class CreateUserCommand : ICommand<UserResult>
{
    public string Name { get; set; }
    public string Email { get; set; }
}

public class UserResult
{
  public Guid UserId { get; set; }
    public bool Success { get; set; }
}

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, UserResult>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserResult> Handle(CreateUserCommand command)
    {
        var user = new User 
        { 
      Id = Guid.NewGuid(), 
            Name = command.Name, 
  Email = command.Email 
        };
        
        await _repository.AddAsync(user);
        
     return new UserResult 
{ 
  UserId = user.Id, 
            Success = true 
     };
  }
}

// Utilisation
var result = await mediator.Send<CreateUserCommand, UserResult>(
    new CreateUserCommand { Name = "Jean", Email = "jean@example.com" }
);

Console.WriteLine($"Utilisateur créé: {result.UserId}");
```

### 2. Queries (Requêtes)

Les queries représentent des **opérations de lecture** qui ne modifient pas l'état.

```csharp
public class GetUserQuery : IQuery<UserDto>
{
    public Guid UserId { get; set; }
}

public class UserDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _repository;

    public GetUserQueryHandler(IUserRepository repository)
    {
      _repository = repository;
    }

  public async Task<UserDto> Handle(GetUserQuery query)
    {
        var user = await _repository.GetByIdAsync(query.UserId);
        
        return new UserDto 
        { 
     Id = user.Id, 
  Name = user.Name, 
   Email = user.Email 
        };
    }
}

// Utilisation
var userDto = await mediator.Send<GetUserQuery, UserDto>(
    new GetUserQuery { UserId = userId }
);

Console.WriteLine($"Utilisateur: {userDto.Name}");
```

### 3. Events (Événements)

Les events sont envoyés pour **notifier** d'un changement d'état.

```csharp
public class UserCreatedEvent : IEvent
{
    public Guid UserId { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class UserCreatedEventHandler : IEventHandler<UserCreatedEvent>
{
    private readonly ILogger<UserCreatedEventHandler> _logger;

    public UserCreatedEventHandler(ILogger<UserCreatedEventHandler> logger)
    {
  _logger = logger;
    }

    public async Task Handle(UserCreatedEvent @event)
    {
        _logger.LogInformation($"Utilisateur créé: {@event.Name}");
    // Effectuer des actions supplémentaires (notifications, etc.)
        await Task.CompletedTask;
    }
}

// Utilisation
await mediator.Publish(new UserCreatedEvent 
{ 
    UserId = user.Id, 
    Name = user.Name, 
    Email = user.Email 
});
```

## Cas d'usage courants

### E-commerce: Placer une commande

```csharp
public class PlaceOrderCommand : ICommand<OrderResult>
{
    public Guid UserId { get; set; }
    public List<OrderItem> Items { get; set; }
}

public class OrderResult
{
    public Guid OrderId { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, OrderResult>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPaymentService _paymentService;
    private readonly Mediator _mediator;

    public async Task<OrderResult> Handle(PlaceOrderCommand command)
    {
        // Créer la commande
     var order = new Order 
  { 
  Id = Guid.NewGuid(),
            UserId = command.UserId,
         Items = command.Items,
  CreatedAt = DateTime.UtcNow
        };

      var totalAmount = order.Items.Sum(i => i.Price * i.Quantity);
   order.TotalAmount = totalAmount;

    // Traiter le paiement
        await _paymentService.ProcessPaymentAsync(command.UserId, totalAmount);

        // Sauvegarder la commande
      await _orderRepository.AddAsync(order);

        // Notifier les autres systèmes
    await _mediator.Publish(new OrderPlacedEvent 
        { 
            OrderId = order.Id, 
            UserId = command.UserId 
      });

        return new OrderResult 
        { 
        OrderId = order.Id,
          TotalAmount = totalAmount
  };
    }
}
```

## Gestion des erreurs

BrilliantMediator lève une `HandlerNotRegisteredException` si aucun handler n'est enregistré:

```csharp
try
{
    await mediator.Send(new SomeCommand());
}
catch (HandlerNotRegisteredException ex)
{
    Console.WriteLine($"Erreur: {ex.Message}");
    // Gérer l'erreur appropriée
}
```

## Performance

BrilliantMediator atteint une performance exceptionnelle (~50ns par opération) grâce à:

- **Zéro réflexion** - Utilise des registres statiques génériques compilés
- **Zéro allocations** - Aucun objet intermédiaire créé
- **Vérification au compile-time** - Toutes les décisions de dispatch sont prises à la compilation
- **JIT Inlining** - Les méthodes sont assez petites pour être inlinées

## Quand utiliser BrilliantMediator

✅ **Utilisez BrilliantMediator si:**
- La performance est critique
- Vous voulez une implémentation simple et minimale
- Vous construisez une architecture CQRS
- Vous voulez éviter les dépendances externes complexes
- Vous avez besoin du pattern Mediator sans surcharge

## API Reference

### Mediator

```csharp
// Envoyer une commande sans réponse
public async Task Send<TCommand>(TCommand command) where TCommand : ICommand

// Envoyer une commande avec réponse
public async Task<TResponse> Send<TCommand, TResponse>(TCommand command) 
    where TCommand : ICommand<TResponse>

// Envoyer une requête
public async Task<TResponse> Send<TQuery, TResponse>(TQuery query) 
    where TQuery : IQuery<TResponse>

// Publier un événement
public async Task Publish<TEvent>(TEvent @event) 
    where TEvent : IEvent

// Enregistrer un handler de commande (sans réponse)
public void RegisterCommandHandler<TCommand>(ICommandHandler<TCommand> handler) 
    where TCommand : ICommand

// Enregistrer un handler de commande (avec réponse)
public void RegisterCommandHandler<TCommand, TResponse>(
    ICommandHandler<TCommand, TResponse> handler) 
    where TCommand : ICommand<TResponse>

// Enregistrer un handler de requête
public void RegisterQueryHandler<TQuery, TResponse>(
    IQueryHandler<TQuery, TResponse> handler) 
    where TQuery : IQuery<TResponse>

// Enregistrer un handler d'événement
public void RegisterEventHandler<TEvent>(IEventHandler<TEvent> handler) 
    where TEvent : IEvent
```

## Ressources

- 📖 [README complet](../README.md)
- 🐛 [Issues](https://github.com/Monbsoft/BrilliantMediator/issues)
- 💬 [Discussions](https://github.com/Monbsoft/BrilliantMediator/discussions)
