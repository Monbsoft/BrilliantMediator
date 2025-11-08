using Radiant.ConsoleApp.Application.Domain;

namespace Radiant.ConsoleApp.Application.Interfaces;

public interface ITodoRepository
{
    Task AddAsync(Todo todo);

    Task UpdateAsync(Todo todo);

    Task DeleteAsync(int id);

    Task<Todo> GetByIdAsync(int id);

    Task<List<Todo>> GetAllAsync();

    Task<int> GetCompletedCountAsync();
}