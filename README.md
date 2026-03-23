# BrilliantMediator

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-10%2B-blue)
![NuGet](https://img.shields.io/badge/NuGet-BrilliantMediator-blue)
![Version](https://img.shields.io/badge/version-3.0.0-blue)

**Ultra-lightweight, zero-reflection mediator for .NET with blazing performance.**

BrilliantMediator is a high-performance implementation of the Mediator pattern that focuses on simplicity and speed.

## ✨ Key Features

- ⚡ **Zero Reflection** - Uses compiled generics, no `typeof()` lookups at runtime
- 🚀 **Blazing Fast** - Overhead < 50ns per operation
- 🎯 **Type-Safe** - Full compile-time checking via generics
- 📦 **Tiny** - ~100 lines of core code
- 🔧 **Simple** - Easy to understand and maintain
- 🌐 **Framework Agnostic** - Works with any .NET host (Console, Worker Service, ASP.NET Core)
- 📋 **CQRS + Events** - Commands, Queries, and parallel Events out of the box
- 🛠️ **Source Generator** - Zero-reflection handler registration generated at compile time

## Packages

| Package | Description |
|---------|-------------|
| `BrilliantMediator` | Core library |
| `BrilliantMediator.SourceGenerator` | Roslyn generator — registers handlers at compile time |

## Installation

```bash
dotnet add package BrilliantMediator
# Optional: auto-register handlers at compile time
dotnet add package BrilliantMediator.SourceGenerator
```

## Quick Start

### 1. Define a Command

```csharp
using Monbsoft.BrilliantMediator.Abstractions.Commands;

public class CreateUserCommand : ICommand<CreateUserResult>
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class CreateUserResult
{
    public Guid UserId { get; set; }
    public bool Success { get; set; }
}
```

### 2. Create a Handler

```csharp
using Monbsoft.BrilliantMediator.Abstractions.Commands;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IUserRepository _repository;

    public CreateUserCommandHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateUserResult> Handle(CreateUserCommand command, CancellationToken cancellationToken = default)
    {
        var user = new User { Id = Guid.NewGuid(), Name = command.Name, Email = command.Email };
        await _repository.AddAsync(user, cancellationToken);
        return new CreateUserResult { UserId = user.Id, Success = true };
    }
}
```

### 3. Configure DI and Use

```csharp
using Monbsoft.BrilliantMediator.Extensions;

// Startup / Program.cs
var services = new ServiceCollection();

services
    .AddBrilliantMediator()
    .AddCommandHandler<CreateUserCommand, CreateUserResult, CreateUserCommandHandler>()
    // chain other handlers...
    .Build();

var serviceProvider = services.BuildServiceProvider();
serviceProvider.UseBrilliantMediator(); // initialize handler registry

// Usage
var mediator = serviceProvider.GetRequiredService<IMediator>();
var result = await mediator.DispatchAsync<CreateUserCommand, CreateUserResult>(
    new CreateUserCommand { Name = "John", Email = "john@example.com" }
);
```

## Concepts

### Commands

Commands represent actions that modify state.

#### Command without Response

```csharp
public class SendEmailCommand : ICommand
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
}

public class SendEmailCommandHandler : ICommandHandler<SendEmailCommand>
{
    public async Task Handle(SendEmailCommand command, CancellationToken cancellationToken = default)
    {
        // Send email...
        await Task.CompletedTask;
    }
}

// Usage
await mediator.DispatchAsync(new SendEmailCommand { To = "user@example.com", Subject = "Hello" });
```

#### Command with Response

```csharp
public class CalculateCommand : ICommand<int>
{
    public int A { get; set; }
    public int B { get; set; }
}

public class CalculateCommandHandler : ICommandHandler<CalculateCommand, int>
{
    public Task<int> Handle(CalculateCommand command, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(command.A + command.B);
    }
}

// Usage
var result = await mediator.DispatchAsync<CalculateCommand, int>(
    new CalculateCommand { A = 5, B = 3 }
);
Console.WriteLine(result); // 8
```

### Queries

Queries represent read operations that don't modify state.

```csharp
using Monbsoft.BrilliantMediator.Abstractions.Queries;

public class GetUserQuery : IQuery<UserDto>
{
    public Guid UserId { get; set; }
}

public class GetUserQueryHandler : IQueryHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _repository;

    public GetUserQueryHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserDto> Handle(GetUserQuery query, CancellationToken cancellationToken = default)
    {
        var user = await _repository.GetByIdAsync(query.UserId, cancellationToken);
        return new UserDto { Id = user.Id, Name = user.Name, Email = user.Email };
    }
}

// Usage
var userDto = await mediator.SendAsync<GetUserQuery, UserDto>(
    new GetUserQuery { UserId = userId }
);
```

### Events (Fire-and-Forget)

Multiple handlers can be registered for the same event — they run in parallel.

```csharp
using Monbsoft.BrilliantMediator.Abstractions.Events;

public class UserCreatedEvent : IEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class SendWelcomeEmailHandler : IEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        // Send welcome email...
        await Task.CompletedTask;
    }
}

public class AuditUserCreationHandler : IEventHandler<UserCreatedEvent>
{
    public async Task Handle(UserCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        // Log audit entry...
        await Task.CompletedTask;
    }
}

// Publish — both handlers run in parallel
await mediator.PublishAsync(new UserCreatedEvent { UserId = userId, Email = "user@example.com" });
```

## Dependency Injection

### Manual registration (fluent builder)

```csharp
services
    .AddBrilliantMediator()
    .AddCommandHandler<CreateUserCommand, UserDto, CreateUserCommandHandler>()
    .AddCommandHandler<DeleteUserCommand, DeleteUserCommandHandler>()
    .AddQueryHandler<GetUserQuery, UserDto, GetUserQueryHandler>()
    .AddEventHandler<UserCreatedEvent, SendWelcomeEmailHandler>()
    .AddEventHandler<UserCreatedEvent, AuditUserCreationHandler>()
    .Build();

var serviceProvider = services.BuildServiceProvider();
serviceProvider.UseBrilliantMediator();
```

### Auto-registration with Source Generator

Add the `BrilliantMediator.SourceGenerator` package to your `.csproj`:

```xml
<PackageReference Include="BrilliantMediator.SourceGenerator"
                  Version="3.0.0"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

The generator scans your assembly at **compile time** and generates an `AddGeneratedHandlers()` extension method. Use it in place of manual registrations:

```csharp
services
    .AddBrilliantMediator()
    .AddGeneratedHandlers()   // generated by BrilliantMediator.SourceGenerator
    .Build();

var serviceProvider = services.BuildServiceProvider();
serviceProvider.UseBrilliantMediator();
```

To scan handlers from additional assemblies, add the attribute:

```csharp
[assembly: BrilliantMediatorGenerator(
    Namespace = "MyApp.Infrastructure.Generated",
    Assemblies = [typeof(MyCommandHandler), typeof(MyQueryHandler)])]
```

The current assembly is always scanned. `Assemblies` lets you include handlers from other referenced assemblies.

### Service lifetimes

```csharp
services
    .AddBrilliantMediator()
    .AddCommandHandler<MyCommand, MyCommandHandler>(ServiceLifetime.Singleton)
    .AddQueryHandler<MyQuery, MyResult, MyQueryHandler>(ServiceLifetime.Transient)
    .Build();
```

Default lifetime is `Scoped`.

## ASP.NET Core

Works without any coupling to `IApplicationBuilder`. Call `UseBrilliantMediator()` on `app.Services`:

```csharp
var app = builder.Build();
app.Services.UseBrilliantMediator();
```

## Why so fast?

1. **No Reflection at runtime** — handler types are stored in a `ConcurrentDictionary` populated at startup
2. **DI scope per call** — each dispatch creates a dedicated `IServiceScope`, ensuring proper scoped service lifetime (e.g., `DbContext`)
3. **`ImmutableList` for events** — lock-free reads for parallel event dispatch
4. **Compile-time type safety** — all generics resolved by the JIT, no dynamic dispatch

## Exception Handling

BrilliantMediator throws `HandlerNotRegisteredException` when a handler is not registered:

```csharp
try
{
    await mediator.DispatchAsync(new SomeCommand());
}
catch (HandlerNotRegisteredException ex)
{
    Console.WriteLine(ex.Message);
    // "No handler registered for command 'SomeCommand'"
}
```

## API Reference

### IMediator

```csharp
// Dispatch command without response
Task DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
    where TCommand : ICommand;

// Dispatch command with response
Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command, CancellationToken cancellationToken = default)
    where TCommand : ICommand<TResponse>;

// Send query
Task<TResponse> SendAsync<TQuery, TResponse>(TQuery query, CancellationToken cancellationToken = default)
    where TQuery : IQuery<TResponse>;

// Publish event (parallel handlers)
Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
    where TEvent : IEvent;
```

### IHandlerRegistry

```csharp
void RegisterCommandHandler<TCommand>() where TCommand : ICommand;
void RegisterCommandHandler<TCommand, TResponse>() where TCommand : ICommand<TResponse>;
void RegisterQueryHandler<TQuery, TResponse>() where TQuery : IQuery<TResponse>;
void RegisterEventHandler<TEvent>() where TEvent : IEvent;
```

> **Note:** `IHandlerRegistry` is called internally by `UseBrilliantMediator()`. Application code should only depend on `IMediator`.

## Supported .NET Versions

- **.NET 10.0**

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Support

- 📖 [Documentation](docs/)
- 🐛 [Issue Tracker](https://github.com/Monbsoft/BrilliantMediator/issues)
- 💬 [Discussions](https://github.com/Monbsoft/BrilliantMediator/discussions)
