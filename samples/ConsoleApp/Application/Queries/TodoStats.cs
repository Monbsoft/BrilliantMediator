namespace Radiant.ConsoleApp.Application.Queries;

public class TodoStats
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Pending { get; set; }
    public decimal CompletionPercentage { get; set; }
}