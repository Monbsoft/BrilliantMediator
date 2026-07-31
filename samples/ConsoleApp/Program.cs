using Microsoft.Extensions.DependencyInjection;
using Monbsoft.BrilliantMediator.Abstractions;
using Monbsoft.BrilliantMediator.Abstractions.Commands;
using Monbsoft.BrilliantMediator.Extensions;
using Radiant.ConsoleApp.Application.Commands;
using Radiant.ConsoleApp.Application.DTOs;
using Radiant.ConsoleApp.Application.Interfaces;
using Radiant.ConsoleApp.Application.Queries;
using Radiant.ConsoleApp.Application.Services;
using Radiant.ConsoleApp.Application.UseCases;
using Radiant.ConsoleApp.Repositories;

Console.WriteLine("🚀 Radiant - Todo Sample App\n");

// Setup DI
var services = new ServiceCollection();
var repository = new InMemoryTodoRepository();

services
    .AddSingleton<ITodoRepository>(repository)
    .AddTransient<TodoService>()
    .AddBrilliantMediator()    
    .AddCommandHandler<CreateTodoCommand, CreateTodoResult, CreateTodoCommandHandler>()
    .AddCommandHandler<CompleteTodoCommand, CompleteTodoResult, CompleteTodoCommandHandler>()
    .AddCommandHandler<DeleteTodoCommand, DeleteTodoCommandHandler>()
    .AddQueryHandler<GetAllTodosQuery, GetAllTodosResult, GetAllTodosQueryHandler>()
    .AddQueryHandler<GetTodoByIdQuery, TodoDto, GetTodoByIdQueryHandler>()
    .AddQueryHandler<GetTodoStatsQuery, TodoStats, GetTodoStatsQueryHandler>()
    .Build();

var provider = services.BuildServiceProvider();
provider.UseBrilliantMediator();
var mediator = provider.GetRequiredService<IMediator>();

var todoService = provider.GetRequiredService<TodoService>();

// Demo
try
{
    await todoService.CreateTodo("Learn BrilliantMediator", "Understand zero-reflection mediator");
    await todoService.CreateTodo("Build Todorior", "Create awesome task management app");
    await todoService.CreateTodo("Optimize performance", "Use BrilliantMediator for speed");

    await todoService.ShowAllTodos();

    await todoService.CompleteTodo(1);
    await todoService.CompleteTodo(2);

    await todoService.ShowAllTodos();

    await todoService.ShowTodoStats();

    await todoService.DeleteTodo(3);

    await todoService.ShowAllTodos();
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error: {ex.Message}");
}

Console.WriteLine("\n✨ Done!");