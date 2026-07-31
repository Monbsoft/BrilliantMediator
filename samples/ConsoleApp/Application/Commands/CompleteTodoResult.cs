namespace Radiant.ConsoleApp.Application.Commands;

public class CompleteTodoResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CompletedCount { get; set; }
}