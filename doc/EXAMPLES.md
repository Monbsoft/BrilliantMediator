# Exemples pratiques BrilliantMediator

Cette page contient des exemples concrets et prêts à utiliser avec BrilliantMediator.

## ⭐ Exemple complet: E-Commerce DDD avec WebAPI

Pour un **exemple de production prêt à l'emploi**, consultez le projet complet:
- 📁 **Localisation**: `samples/EcommerceDDD/`
- 📖 **Documentation**: `samples/EcommerceDDD/README.md`
- 🏛️ **Architecture**: `samples/EcommerceDDD/ARCHITECTURE.md`

### Contenu du projet

✅ **Architecture DDD complète** avec 4 assemblies:
- `EcommerceDDD.Domain` - Logique métier (Agrégats, Value Objects, Services de domaine)
- `EcommerceDDD.Application` - Commands, Queries, Handlers, DTOs
- `EcommerceDDD.Infrastructure` - Repositories, Injection de dépendances
- `EcommerceDDD.Api` - WebAPI REST avec Swagger

✅ **Patterns implémentés**:
- Agrégats racine (Order)
- Value Objects (OrderItem)
- Domain Services (OrderDomainService)
- Repositories (IOrderRepository)
- Domain Events (OrderPlaced, OrderConfirmed, etc.)
- Event Handlers

✅ **Fonctionnalités**:
- Placement de commandes
- Gestion du statut des commandes (Pending → Confirmed → Shipped → Delivered)
- Annulation de commandes
- Récupération des commandes
- Gestion des utilisateurs

✅ **API REST complète** avec Swagger/OpenAPI
✅ **Logging avec Serilog**
✅ **Repository en mémoire** (facilement remplaçable par Entity Framework Core)

### Démarrer le projet

```bash
cd samples/EcommerceDDD/EcommerceDDD.Api
dotnet run
```

Puis ouvrez http://localhost:5000/swagger pour explorer l'API.

---

## Table des matières

1. [Exemple 1: Blog - Gestion d'articles](#blog---gestion-darticles)
2. [Exemple 2: Todo App - Liste de tâches](#todo-app---liste-de-tâches)
3. [Exemple 3: E-Commerce - Gestion de commandes](#e-commerce---gestion-de-commandes) (version simplifiée)
4. [Exemple 4: Notification System - Système de notifications](#notification-system---système-de-notifications)

---

## Blog - Gestion d'articles

### Modèles

```csharp
namespace Blog.Models
{
    public class Article
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
        public DateTime CreatedAt { get; set; }
   public int ViewCount { get; set; }
    }

    public class ArticleDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public int ViewCount { get; set; }
    }
}
```

### Commands

```csharp
namespace Blog.Features.Articles.Commands
{
    // Créer un article
    public class CreateArticleCommand : ICommand<CreateArticleResult>
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string Author { get; set; }
    }

    public class CreateArticleResult
    {
        public Guid ArticleId { get; set; }
        public bool Success { get; set; }
    }

    public class CreateArticleCommandHandler : ICommandHandler<CreateArticleCommand, CreateArticleResult>
    {
        private readonly IArticleRepository _repository;
      private readonly Mediator _mediator;

        public CreateArticleCommandHandler(IArticleRepository repository, Mediator mediator)
        {
         _repository = repository;
            _mediator = mediator;
        }

        public async Task<CreateArticleResult> Handle(CreateArticleCommand command)
        {
            var article = new Article
            {
     Id = Guid.NewGuid(),
                Title = command.Title,
    Content = command.Content,
     Author = command.Author,
        CreatedAt = DateTime.UtcNow,
      ViewCount = 0
    };

     await _repository.AddAsync(article);

            // Publier un événement
            await _mediator.Publish(new ArticleCreatedEvent 
       { 
   ArticleId = article.Id, 
     Title = article.Title,
         Author = article.Author
      });

            return new CreateArticleResult 
          { 
             ArticleId = article.Id, 
     Success = true 
   };
  }
    }

    // Supprimer un article
    public class DeleteArticleCommand : ICommand
    {
        public Guid ArticleId { get; set; }
    }

    public class DeleteArticleCommandHandler : ICommandHandler<DeleteArticleCommand>
    {
 private readonly IArticleRepository _repository;
        private readonly Mediator _mediator;

      public async Task Handle(DeleteArticleCommand command)
        {
   var article = await _repository.GetByIdAsync(command.ArticleId);
      
         if (article == null)
         throw new ArticleNotFoundException(command.ArticleId);

 await _repository.DeleteAsync(article);

  await _mediator.Publish(new ArticleDeletedEvent 
            { 
        ArticleId = article.Id, 
   Title = article.Title 
});
  }
    }
}
```

### Queries

```csharp
namespace Blog.Features.Articles.Queries
{
    // Récupérer tous les articles
    public class GetAllArticlesQuery : IQuery<List<ArticleDto>>
    {
    }

    public class GetAllArticlesQueryHandler : IQueryHandler<GetAllArticlesQuery, List<ArticleDto>>
    {
      private readonly IArticleRepository _repository;

        public async Task<List<ArticleDto>> Handle(GetAllArticlesQuery query)
        {
   var articles = await _repository.GetAllAsync();
        
     return articles
          .Select(a => new ArticleDto
        {
     Id = a.Id,
                    Title = a.Title,
    Author = a.Author,
    ViewCount = a.ViewCount
    })
    .ToList();
        }
    }

    // Récupérer un article par ID
  public class GetArticleByIdQuery : IQuery<ArticleDto>
    {
        public Guid ArticleId { get; set; }
    }

    public class GetArticleByIdQueryHandler : IQueryHandler<GetArticleByIdQuery, ArticleDto>
    {
      private readonly IArticleRepository _repository;

        public async Task<ArticleDto> Handle(GetArticleByIdQuery query)
      {
            var article = await _repository.GetByIdAsync(query.ArticleId);
            
      if (article == null)
          throw new ArticleNotFoundException(query.ArticleId);

            return new ArticleDto
            {
      Id = article.Id,
    Title = article.Title,
        Author = article.Author,
       ViewCount = article.ViewCount
     };
        }
    }
}
```

### Events

```csharp
namespace Blog.Features.Articles.Events
{
    public class ArticleCreatedEvent : IEvent
    {
        public Guid ArticleId { get; set; }
        public string Title { get; set; }
     public string Author { get; set; }
    }

    public class ArticleCreatedEventHandler : IEventHandler<ArticleCreatedEvent>
    {
        private readonly ILogger<ArticleCreatedEventHandler> _logger;
        private readonly INotificationService _notificationService;

        public ArticleCreatedEventHandler(ILogger<ArticleCreatedEventHandler> logger, 
        INotificationService notificationService)
        {
        _logger = logger;
            _notificationService = notificationService;
        }

        public async Task Handle(ArticleCreatedEvent @event)
  {
     _logger.LogInformation($"Article créé: {@event.Title} par {@event.Author}");
   
   // Notifier les utilisateurs
    await _notificationService.NotifyNewArticleAsync(@event.Title);
      }
    }

    public class ArticleDeletedEvent : IEvent
    {
        public Guid ArticleId { get; set; }
        public string Title { get; set; }
    }

    public class ArticleDeletedEventHandler : IEventHandler<ArticleDeletedEvent>
    {
        private readonly ILogger<ArticleDeletedEventHandler> _logger;

        public async Task Handle(ArticleDeletedEvent @event)
        {
     _logger.LogWarning($"Article supprimé: {@event.Title}");
    await Task.CompletedTask;
        }
    }
}
```

### Utilisation

```csharp
// Configuration
var services = new ServiceCollection();
services.AddBrilliantMediator(typeof(Program).Assembly);
var provider = services.BuildServiceProvider();
var mediator = provider.GetRequiredService<Mediator>();

// Créer un article
var createResult = await mediator.Send<CreateArticleCommand, CreateArticleResult>(
  new CreateArticleCommand
    {
        Title = "Bienvenue sur mon blog",
        Content = "Contenu de l'article...",
        Author = "Jean Dupont"
    }
);

// Récupérer tous les articles
var articles = await mediator.Send<GetAllArticlesQuery, List<ArticleDto>>(
    new GetAllArticlesQuery()
);

// Récupérer un article spécifique
var article = await mediator.Send<GetArticleByIdQuery, ArticleDto>(
    new GetArticleByIdQuery { ArticleId = createResult.ArticleId }
);

// Supprimer un article
await mediator.Send(new DeleteArticleCommand { ArticleId = createResult.ArticleId });
```

---

## Todo App - Liste de tâches

### Modèles et Commands

```csharp
namespace TodoApp.Features
{
    public class TodoItem
    {
 public Guid Id { get; set; }
     public string Title { get; set; }
        public bool IsCompleted { get; set; }
     public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

// Ajouter une tâche
    public class AddTodoCommand : ICommand<Guid>
    {
        public string Title { get; set; }
    }

    public class AddTodoCommandHandler : ICommandHandler<AddTodoCommand, Guid>
    {
        private readonly ITodoRepository _repository;

        public async Task<Guid> Handle(AddTodoCommand command)
     {
  var todo = new TodoItem
            {
    Id = Guid.NewGuid(),
  Title = command.Title,
 IsCompleted = false,
 CreatedAt = DateTime.UtcNow
  };

            await _repository.AddAsync(todo);
       return todo.Id;
        }
    }

    // Marquer comme complété
    public class CompleteTodoCommand : ICommand
    {
     public Guid TodoId { get; set; }
    }

 public class CompleteTodoCommandHandler : ICommandHandler<CompleteTodoCommand>
    {
  private readonly ITodoRepository _repository;

        public async Task Handle(CompleteTodoCommand command)
        {
            var todo = await _repository.GetByIdAsync(command.TodoId);
            
          if (todo == null)
       throw new TodoNotFoundException(command.TodoId);

            todo.IsCompleted = true;
   todo.CompletedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(todo);
     }
    }

    // Récupérer les tâches
 public class GetTodosQuery : IQuery<List<TodoItem>>
    {
        public bool? OnlyCompleted { get; set; }
    }

    public class GetTodosQueryHandler : IQueryHandler<GetTodosQuery, List<TodoItem>>
    {
        private readonly ITodoRepository _repository;

        public async Task<List<TodoItem>> Handle(GetTodosQuery query)
        {
          var todos = await _repository.GetAllAsync();

  if (query.OnlyCompleted.HasValue)
                todos = todos.Where(t => t.IsCompleted == query.OnlyCompleted.Value).ToList();

         return todos;
    }
    }
}
```

---

## E-Commerce - Gestion de commandes

### Modèles

```csharp
namespace ECommerce.Models
{
    public class OrderItem
    {
  public string ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
   public decimal Total => Price * Quantity;
    }

    public class Order
 {
        public Guid Id { get; set; }
   public Guid UserId { get; set; }
      public List<OrderItem> Items { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum OrderStatus
    {
 Pending,
        Confirmed,
        Shipped,
        Delivered,
    Cancelled
    }
}
```

### Placer une commande

```csharp
namespace ECommerce.Features.Orders
{
    public class PlaceOrderCommand : ICommand<PlaceOrderResult>
  {
        public Guid UserId { get; set; }
        public List<OrderItem> Items { get; set; }
    }

    public class PlaceOrderResult
    {
        public Guid OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
    }

    public class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentService _paymentService;
private readonly IInventoryService _inventoryService;
        private readonly Mediator _mediator;

        public async Task<PlaceOrderResult> Handle(PlaceOrderCommand command)
        {
            // Vérifier l'inventaire
        var hasStock = await _inventoryService.CheckAvailabilityAsync(command.Items);
  if (!hasStock)
  throw new InsufficientStockException();

            // Créer la commande
        var order = new Order
   {
             Id = Guid.NewGuid(),
   UserId = command.UserId,
        Items = command.Items,
      TotalAmount = command.Items.Sum(i => i.Total),
    Status = OrderStatus.Pending,
          CreatedAt = DateTime.UtcNow
            };

            // Traiter le paiement
            var paymentResult = await _paymentService.ProcessPaymentAsync(
       command.UserId,
     order.TotalAmount
        );

            if (!paymentResult.Success)
                throw new PaymentFailedException();

   // Réserver l'inventaire
            await _inventoryService.ReserveAsync(command.Items);

    order.Status = OrderStatus.Confirmed;
            await _orderRepository.AddAsync(order);

            // Publier l'événement
            await _mediator.Publish(new OrderPlacedEvent
      {
    OrderId = order.Id,
  UserId = command.UserId,
    TotalAmount = order.TotalAmount,
                Items = command.Items
      });

            return new PlaceOrderResult
      {
      OrderId = order.Id,
       TotalAmount = order.TotalAmount,
          Status = order.Status
            };
        }
    }

    public class OrderPlacedEvent : IEvent
    {
        public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }
 public List<OrderItem> Items { get; set; }
    }

    public class OrderPlacedEventHandler : IEventHandler<OrderPlacedEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<OrderPlacedEventHandler> _logger;

        public async Task Handle(OrderPlacedEvent @event)
        {
     _logger.LogInformation($"Commande placée: {@event.OrderId}");

         // Envoyer un email de confirmation
            await _emailService.SendOrderConfirmationAsync(
           @event.UserId,
   @event.OrderId,
                @event.Items
      );
        }
    }
}
```

---

## Notification System - Système de notifications

### Events et Handlers

```csharp
namespace NotificationSystem
{
    // Events
    public class EmailNotificationEvent : IEvent
  {
    public string To { get; set; }
public string Subject { get; set; }
        public string Body { get; set; }
    }

    public class SMSNotificationEvent : IEvent
    {
        public string PhoneNumber { get; set; }
        public string Message { get; set; }
    }

    public class PushNotificationEvent : IEvent
    {
        public Guid UserId { get; set; }
   public string Title { get; set; }
        public string Message { get; set; }
    }

    // Email Handler
    public class EmailNotificationEventHandler : IEventHandler<EmailNotificationEvent>
    {
        private readonly IEmailService _emailService;
        private readonly ILogger<EmailNotificationEventHandler> _logger;

        public async Task Handle(EmailNotificationEvent @event)
        {
    try
            {
         await _emailService.SendAsync(@event.To, @event.Subject, @event.Body);
                _logger.LogInformation($"Email envoyé à {@event.To}");
      }
            catch (Exception ex)
            {
_logger.LogError(ex, $"Erreur lors de l'envoi de l'email à {@event.To}");
     throw;
            }
   }
    }

    // SMS Handler
    public class SMSNotificationEventHandler : IEventHandler<SMSNotificationEvent>
    {
        private readonly ISMSService _smsService;
        private readonly ILogger<SMSNotificationEventHandler> _logger;

        public async Task Handle(SMSNotificationEvent @event)
        {
     try
            {
         await _smsService.SendAsync(@event.PhoneNumber, @event.Message);
        _logger.LogInformation($"SMS envoyé à {@event.PhoneNumber}");
   }
            catch (Exception ex)
        {
       _logger.LogError(ex, $"Erreur lors de l'envoi du SMS à {@event.PhoneNumber}");
       throw;
  }
        }
    }

    // Push Notification Handler
    public class PushNotificationEventHandler : IEventHandler<PushNotificationEvent>
    {
   private readonly IPushNotificationService _pushService;
        private readonly ILogger<PushNotificationEventHandler> _logger;

        public async Task Handle(PushNotificationEvent @event)
        {
         try
   {
  await _pushService.SendAsync(
     @event.UserId,
      @event.Title,
          @event.Message
          );
_logger.LogInformation($"Push envoyé à l'utilisateur {@event.UserId}");
    }
          catch (Exception ex)
      {
         _logger.LogError(ex, $"Erreur lors de l'envoi du push à {@event.UserId}");
  throw;
            }
        }
    }

    // Commandes de notification
    public class SendNotificationCommand : ICommand
    {
     public Guid UserId { get; set; }
        public string Email { get; set; }
   public string PhoneNumber { get; set; }
        public string Message { get; set; }
    }

    public class SendNotificationCommandHandler : ICommandHandler<SendNotificationCommand>
    {
        private readonly Mediator _mediator;

        public async Task Handle(SendNotificationCommand command)
        {
   // Envoyer email
            if (!string.IsNullOrEmpty(command.Email))
            {
 await _mediator.Publish(new EmailNotificationEvent
     {
         To = command.Email,
  Subject = "Notification",
      Body = command.Message
     });
            }

    // Envoyer SMS
   if (!string.IsNullOrEmpty(command.PhoneNumber))
        {
       await _mediator.Publish(new SMSNotificationEvent
     {
  PhoneNumber = command.PhoneNumber,
           Message = command.Message
         });
            }

   // Envoyer Push
            await _mediator.Publish(new PushNotificationEvent
         {
     UserId = command.UserId,
      Title = "Notification",
         Message = command.Message
            });
        }
    }
}
```

---

## Conclusion

Ces exemples montrent comment utiliser BrilliantMediator dans différents contextes réels. Les patterns utilisés ici sont réutilisables pour vos propres projets.

### Takeaways clés

1. ✅ Séparez les commands, queries et events
2. ✅ Un handler = une responsabilité
3. ✅ Utilisez les events pour la communication inter-domaines
4. ✅ Injectez les services nécessaires dans les handlers
5. ✅ Levez des exceptions significatives
6. ✅ Loggez les opérations importantes
