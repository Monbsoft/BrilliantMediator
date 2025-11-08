namespace Radiant.ConsoleApp.Application.Commands;

public class CompleteTodoResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public int CompletedCount { get; set; }
}