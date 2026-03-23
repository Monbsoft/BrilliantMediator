# BrilliantMediator

**Ultra-lightweight, zero-reflection mediator for .NET with blazing performance**

[![NuGet](https://img.shields.io/nuget/v/BrilliantMediator?style=flat-square)](https://www.nuget.org/packages/BrilliantMediator)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg?style=flat-square)](LICENSE)
[![GitHub](https://img.shields.io/badge/GitHub-BrilliantMediator-blue?style=flat-square&logo=github)](https://github.com/Monbsoft/BrilliantMediator)

## What is BrilliantMediator?

BrilliantMediator is an extremely lightweight implementation of the **Mediator Pattern** with full **CQRS** and **Event support**.

Designed for **maximum performance** with **zero reflection at runtime**, it provides a type-safe API with compile-time verification.

## Key Features

✨ **Zero-Reflection Architecture** — All handler lookup via `ConcurrentDictionary`, no `GetType()` or assembly scanning at runtime
⚡ **Blazing Fast** — ~50ns per operation overhead
🎯 **Full CQRS Support** — Commands, Queries, and Events
📦 **Parallel Events** — Multiple handlers per event, executed in parallel with isolated DI scopes
🔐 **Type-Safe** — Compile-time verification via generics
🔌 **DI Scoped per call** — Each dispatch creates its own `IServiceScope` (safe for `DbContext`, etc.)
🛠️ **Source Generator** — `BrilliantMediator.SourceGenerator` generates handler registration at compile time
🌐 **Framework Agnostic** — Console, Worker Service, ASP.NET Core — no coupling to `IApplicationBuilder`

## Quick Start

### 1. Install

```bash
dotnet add package BrilliantMediator
# Optional: zero-reflection handler auto-registration
dotnet add package BrilliantMediator.SourceGenerator
```

### 2. Configure DI

```csharp
using Monbsoft.BrilliantMediator.Extensions;

services
    .AddBrilliantMediator()
    .AddCommandHandler<CreateOrderCommand, OrderDto, CreateOrderCommandHandler>()
    .AddQueryHandler<GetOrderQuery, OrderDto, GetOrderQueryHandler>()
    .AddEventHandler<OrderConfirmedEvent, SendConfirmationEmailHandler>()
    .Build();

var serviceProvider = services.BuildServiceProvider();
serviceProvider.UseBrilliantMediator();
```

### 2b. Or use the Source Generator (zero boilerplate)

Add to your `.csproj`:

```xml
<PackageReference Include="BrilliantMediator.SourceGenerator"
                  Version="3.0.0"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

Then:

```csharp
services
    .AddBrilliantMediator()
    .AddGeneratedHandlers()   // generated at compile time — no reflection
    .Build();

serviceProvider.UseBrilliantMediator();
```

### 3. Define Handlers

```csharp
// Command with response
public class CreateOrderCommand : ICommand<OrderDto>
{
    public Guid CustomerId { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _repository;

    public CreateOrderCommandHandler(IOrderRepository repository)
    {
        _repository = repository;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        var order = await _repository.CreateAsync(command.CustomerId, command.Items, cancellationToken);
        return new OrderDto { Id = order.Id, Total = order.Total };
    }
}
```

### 4. Dispatch

```csharp
var mediator = serviceProvider.GetRequiredService<IMediator>();

// Command with response
var order = await mediator.DispatchAsync<CreateOrderCommand, OrderDto>(command, cancellationToken);

// Command without response
await mediator.DispatchAsync(new SendEmailCommand { To = "user@example.com" }, cancellationToken);

// Query
var result = await mediator.SendAsync<GetOrderQuery, OrderDto>(query, cancellationToken);

// Event (all handlers run in parallel)
await mediator.PublishAsync(new OrderConfirmedEvent { OrderId = order.Id }, cancellationToken);
```

## Handler Interfaces

```csharp
// Command without response
public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task Handle(TCommand command, CancellationToken cancellationToken = default);
}

// Command with response
public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken cancellationToken = default);
}

// Query
public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken cancellationToken = default);
}

// Event
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task Handle(TEvent @event, CancellationToken cancellationToken = default);
}
```

## API Reference

### IMediator

```csharp
Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
    where TCommand : ICommand;

Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
    where TCommand : ICommand<TResponse>;

Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
    where TQuery : IQuery<TResponse>;

Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    where TEvent : IEvent;
```

## Performance

- **~50ns overhead** per mediator call
- **O(1) lookup** via `ConcurrentDictionary`
- **No reflection at runtime** — handler types registered explicitly at startup or via Source Generator
- **Isolated DI scope per call** — safe for scoped services (`DbContext`, unit-of-work, etc.)

## Supported .NET Versions

- **.NET 10.0**

## License

MIT — see [LICENSE](https://github.com/Monbsoft/BrilliantMediator/blob/main/LICENSE)

## Author

Created by **[Monbsoft](https://github.com/Monbsoft)**.

---

⭐ If you find this project helpful, please give it a star on [GitHub](https://github.com/Monbsoft/BrilliantMediator)!
