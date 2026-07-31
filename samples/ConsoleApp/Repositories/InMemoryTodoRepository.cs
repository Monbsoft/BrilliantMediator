using Radiant.ConsoleApp.Application.Domain;
using Radiant.ConsoleApp.Application.Interfaces;

namespace Radiant.ConsoleApp.Repositories;

public class InMemoryTodoRepository : ITodoRepository
{
    private readonly List<Todo> _todos = new();
    private int _nextId = 1;

    public Task AddAsync(Todo todo)
    {
        todo.Id = _nextId++;
        todo.CreatedAt = DateTime.UtcNow;
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

    public Task DeleteAsync(int id)
    {
        _todos.RemoveAll(t => t.Id == id);
        return Task.CompletedTask;
    }

    public Task<Todo?> GetByIdAsync(int id)
    {
        var todo = _todos.Find(t => t.Id == id);
        return Task.FromResult(todo);
    }

    public Task<List<Todo>> GetAllAsync()
    {
        return Task.FromResult(new List<Todo>(_todos));
    }

    public Task<int> GetCompletedCountAsync()
    {
        var count = _todos.Count(t => t.Status == TodoStatus.Completed);
        return Task.FromResult(count);
    }
}