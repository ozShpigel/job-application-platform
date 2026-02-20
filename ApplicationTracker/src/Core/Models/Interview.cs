namespace ApplicationTracker.Core.Models;

public sealed record Interview
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Guid ApplicationId { get; init; }
    public required DateTime ScheduledAt { get; init; }
    public required string Type { get; init; } // "Phone", "Technical", "Final", "HR"
    public string? Interviewer { get; init; }
    public string? Topics { get; init; }
    public string? Notes { get; init; }
    public string? Feedback { get; init; }
    public bool Completed { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
