using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Core;
using Radiant.ConsoleApp.Application.Commands;
using Radiant.ConsoleApp.Application.Domain;
using Radiant.ConsoleApp.Application.Queries;

namespace Radiant.ConsoleApp.Application.Services;

public class TodoService
{
    private readonly IMediator _mediator;

    public TodoService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task CreateTodo(string title, string description)
    {
        var result = await _mediator.DispatchAsync<CreateTodoCommand, CreateTodoResult>(
            new CreateTodoCommand { Title = title, Description = description });

        Console.WriteLine($"✅ {result.Message} (ID: {result.TodoId})");
    }

    public async Task CompleteTodo(int todoId)
    {
        var result = await _mediator.DispatchAsync<CompleteTodoCommand, CompleteTodoResult>(
            new CompleteTodoCommand { TodoId = todoId });

        Console.WriteLine(result.Success
            ? $"✅ {result.Message} (Total completed: {result.CompletedCount})"
            : $"❌ {result.Message}");
    }

    public async Task DeleteTodo(int todoId)
    {
        await _mediator.DispatchAsync(new DeleteTodoCommand { TodoId = todoId });
        Console.WriteLine($"✅ Todo {todoId} deleted");
    }

    public async Task ShowAllTodos(TodoStatus? status = null)
    {
        var result = await _mediator.SendAsync<GetAllTodosQuery, GetAllTodosResult>(
            new GetAllTodosQuery { FilterByStatus = status });

        Console.WriteLine($"\n📋 Todos ({result.TotalCount} total, {result.CompletedCount} completed):");
        foreach (var todo in result.Todos)
        {
            Console.WriteLine($"   [{todo.Id}] {todo.Title} - {todo.Status}");
        }
    }

    public async Task ShowTodoStats()
    {
        var stats = await _mediator.SendAsync<GetTodoStatsQuery, TodoStats>(
            new GetTodoStatsQuery());

        Console.WriteLine($"\n📊 Stats:");
        Console.WriteLine($"   Total: {stats.Total}");
        Console.WriteLine($"   Completed: {stats.Completed}");
        Console.WriteLine($"   In Progress: {stats.InProgress}");
        Console.WriteLine($"   Pending: {stats.Pending}");
        Console.WriteLine($"   Completion: {stats.CompletionPercentage:F1}%");
    }
}