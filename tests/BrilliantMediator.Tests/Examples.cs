using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Abstractions.Queries;
using Monbsoft.BrilliantMediator.Core;

namespace Monbsoft.BrilliantMediator.Tests;

/// <summary>
/// Command to create a todo without returning anything.
/// </summary>
public class CreateTodoCommand : ICommand
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Handler for CreateTodoCommand.
/// </summary>
public class CreateTodoCommandHandler : ICommandHandler<CreateTodoCommand>
{
    private readonly ITodoRepository _repository;

    public CreateTodoCommandHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(CreateTodoCommand command, CancellationToken cancellationToken = default)
    {
        var todo = new Todo
        {
            Id = Guid.NewGuid(),
            Title = command.Title,
            Description = command.Description,
            Status = TodoStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(todo);
    }
}

// ============================================================================
// EXAMPLE 2: COMMAND WITH RESPONSE
// ============================================================================

/// <summary>
/// Command to complete a todo and return the result.
/// </summary>
public class CompleteTodoCommand : ICommand<CompleteTodoResult>
{
    public Guid TodoId { get; set; }
}

/// <summary>
/// Response for CompleteTodoCommand.
/// </summary>
public class CompleteTodoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CompletedCount { get; set; }
}

/// <summary>
/// Handler for CompleteTodoCommand.
/// </summary>
public class CompleteTodoCommandHandler : ICommandHandler<CompleteTodoCommand, CompleteTodoResult>
{
    private readonly ITodoRepository _repository;

    public CompleteTodoCommandHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<CompleteTodoResult> Handle(CompleteTodoCommand command, CancellationToken cancellationToken = default)
    {
        var todo = await _repository.GetByIdAsync(command.TodoId);
        if (todo == null)
            return new CompleteTodoResult
            {
                Success = false,
                Message = $"Todo {command.TodoId} not found"
            };

        todo.Status = TodoStatus.Completed;
        todo.CompletedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(todo);

        var completedCount = await _repository.CountCompletedAsync();

        return new CompleteTodoResult
        {
            Success = true,
            Message = "Todo completed successfully",
            CompletedCount = completedCount
        };
    }
}

// ============================================================================
// EXAMPLE 3: QUERY
// ============================================================================

/// <summary>
/// Query to get all todos.
/// </summary>
public class GetAllTodosQuery : IQuery<GetAllTodosResult>
{
    public TodoStatus? FilterByStatus { get; set; }
}

/// <summary>
/// Response for GetAllTodosQuery.
/// </summary>
public class GetAllTodosResult
{
    public List<TodoDto> Todos { get; set; } = new();
    public int TotalCount { get; set; }
    public int CompletedCount { get; set; }
}

/// <summary>
/// Handler for GetAllTodosQuery.
/// </summary>
public class GetAllTodosQueryHandler : IQueryHandler<GetAllTodosQuery, GetAllTodosResult>
{
    private readonly ITodoRepository _repository;

    public GetAllTodosQueryHandler(ITodoRepository repository)
    {
        _repository = repository;
    }

    public async Task<GetAllTodosResult> Handle(GetAllTodosQuery query, CancellationToken cancellationToken = default)
    {
        var todos = await _repository.GetAllAsync();

        if (query.FilterByStatus.HasValue)
        {
            todos = todos.FindAll(t => t.Status == query.FilterByStatus.Value);
        }

        var completedCount = todos.FindAll(t => t.Status == TodoStatus.Completed).Count;

        return new GetAllTodosResult
        {
            Todos = todos.ConvertAll(t => new TodoDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status.ToString(),
                CreatedAt = t.CreatedAt,
                CompletedAt = t.CompletedAt
            }),
            TotalCount = todos.Count,
            CompletedCount = completedCount
        };
    }
}

// ============================================================================
// SUPPORTING TYPES
// ============================================================================

public enum TodoStatus
{
    Pending,
    InProgress,
    Completed
}

public class Todo
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TodoStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class TodoDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public interface ITodoRepository
{
    Task AddAsync(Todo todo);
    Task UpdateAsync(Todo todo);
    Task<Todo?> GetByIdAsync(Guid id);
    Task<List<Todo>> GetAllAsync();
    Task<int> CountCompletedAsync();
}

public class InMemoryTodoRepository : ITodoRepository
{
    private readonly List<Todo> _todos = new();

    public Task AddAsync(Todo todo)
    {
        _todos.Add(todo);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Todo todo)
    {
        var index = _todos.FindIndex(t => t.Id == todo.Id);
        if (index >= 0)
            _todos[index] = todo;
        return Task.CompletedTask;
    }

    public Task<Todo?> GetByIdAsync(Guid id)
    {
        var todo = _todos.Find(t => t.Id == id);
        return Task.FromResult(todo);
    }

    public Task<List<Todo>> GetAllAsync()
    {
        return Task.FromResult(new List<Todo>(_todos));
    }

    public Task<int> CountCompletedAsync()
    {
        var count = _todos.Count(t => t.Status == TodoStatus.Completed);
        return Task.FromResult(count);
    }
}

// ============================================================================
// USAGE EXAMPLE
// ============================================================================

public class Program
{
    // Named RunAsync rather than Main: a test assembly already has a generated
    // entry point, and a second one only triggers CS8892 (this method is a
    // documentation sample, never invoked).
    public static async Task RunAsync(string[] args)
    {
        var (mediator, serviceProvider) = TestMediatorFactory.Create();
        var repository = new InMemoryTodoRepository();

        var createTodoHandler = new CreateTodoCommandHandler(repository);
        var completeTodoHandler = new CompleteTodoCommandHandler(repository);
        var getAllTodosHandler = new GetAllTodosQueryHandler(repository);

        serviceProvider.AddCommandHandler(createTodoHandler);
        serviceProvider.AddCommandHandler<CompleteTodoCommand, CompleteTodoResult>(completeTodoHandler);
        serviceProvider.AddQueryHandler<GetAllTodosQuery, GetAllTodosResult>(getAllTodosHandler);

        mediator.RegisterCommandHandler<CreateTodoCommand>();
        mediator.RegisterCommandHandler<CompleteTodoCommand, CompleteTodoResult>();
        mediator.RegisterQueryHandler<GetAllTodosQuery, GetAllTodosResult>();

        await mediator.DispatchAsync(new CreateTodoCommand
        {
            Title = "Learn BrilliantMediator",
            Description = "Understand the zero-reflection mediator pattern"
        });

        await mediator.DispatchAsync(new CreateTodoCommand
        {
            Title = "Build TodoApp",
            Description = "Create an awesome task management app"
        });

        var allTodos = await mediator.SendAsync<GetAllTodosQuery, GetAllTodosResult>(
            new GetAllTodosQuery());

        Console.WriteLine($"Total todos: {allTodos.TotalCount}");
        foreach (var todo in allTodos.Todos)
        {
            Console.WriteLine($"  - {todo.Title} ({todo.Status})");
        }

        var todos = await repository.GetAllAsync();
        if (todos.Count > 0)
        {
            var result = await mediator.DispatchAsync<CompleteTodoCommand, CompleteTodoResult>(
                new CompleteTodoCommand { TodoId = todos[0].Id });

            Console.WriteLine($"\n{result.Message}");
            Console.WriteLine($"Completed todos: {result.CompletedCount}");
        }
    }
}
