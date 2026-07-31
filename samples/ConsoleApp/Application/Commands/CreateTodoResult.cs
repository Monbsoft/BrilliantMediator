namespace Radiant.ConsoleApp.Application.Commands;

public class CreateTodoResult
{
    public int TodoId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}